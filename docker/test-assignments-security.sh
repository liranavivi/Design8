#!/bin/bash

# AssignmentsController Security Testing Script
# Tests for common security vulnerabilities and attack vectors

set -e

# Configuration
API_BASE_URL="http://localhost:5130"  # Local development URL
API_ENDPOINT="$API_BASE_URL/api/assignments"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test counter
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0
WARNINGS=0

# Function to print test results
print_test_result() {
    local test_name="$1"
    local result="$2"
    local details="$3"
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    case "$result" in
        "PASS")
            echo -e "${GREEN}✓ PASS${NC} - $test_name"
            PASSED_TESTS=$((PASSED_TESTS + 1))
            ;;
        "FAIL")
            echo -e "${RED}✗ FAIL${NC} - $test_name"
            FAILED_TESTS=$((FAILED_TESTS + 1))
            ;;
        "WARNING")
            echo -e "${YELLOW}⚠ WARNING${NC} - $test_name"
            WARNINGS=$((WARNINGS + 1))
            ;;
    esac
    
    if [ ! -z "$details" ]; then
        echo -e "${YELLOW}Details:${NC} $details"
    fi
}

# Function to test SQL injection attempts
test_sql_injection() {
    echo -e "${BLUE}=== Testing SQL Injection Resistance ===${NC}"
    
    # SQL injection payloads
    local sql_payloads=(
        "'; DROP TABLE assignments; --"
        "' OR '1'='1"
        "' UNION SELECT * FROM users --"
        "'; INSERT INTO assignments VALUES ('malicious'); --"
        "' OR 1=1 --"
    )
    
    for payload in "${sql_payloads[@]}"; do
        # Test in name parameter
        response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/by-name/$(echo "$payload" | sed 's/ /%20/g')" 2>/dev/null || echo "000")
        status_code="${response: -3}"
        
        if [ "$status_code" = "500" ]; then
            print_test_result "SQL Injection in name parameter" "FAIL" "Server error suggests possible SQL injection vulnerability"
        else
            print_test_result "SQL Injection in name parameter" "PASS" "Properly handled malicious input"
        fi
    done
}

# Function to test XSS attempts
test_xss_attacks() {
    echo -e "${BLUE}=== Testing XSS Resistance ===${NC}"
    
    # XSS payloads
    local xss_payloads=(
        "<script>alert('xss')</script>"
        "<img src=x onerror=alert('xss')>"
        "javascript:alert('xss')"
        "<svg onload=alert('xss')>"
    )
    
    for payload in "${xss_payloads[@]}"; do
        # Test in POST request
        response=$(curl -s -w "%{http_code}" -X POST \
            -H "Content-Type: application/json" \
            -d "{
                \"version\": \"1.0.0\",
                \"name\": \"$payload\",
                \"stepId\": \"123e4567-e89b-12d3-a456-426614174001\",
                \"entityIds\": [\"123e4567-e89b-12d3-a456-426614174002\"]
            }" \
            "$API_ENDPOINT" 2>/dev/null || echo "000")
        
        status_code="${response: -3}"
        response_body="${response%???}"
        
        # Check if XSS payload is reflected unescaped
        if echo "$response_body" | grep -F "$payload" > /dev/null; then
            print_test_result "XSS in POST response" "WARNING" "XSS payload reflected in response"
        else
            print_test_result "XSS in POST response" "PASS" "XSS payload properly handled"
        fi
    done
}

# Function to test HTTP headers security
test_security_headers() {
    echo -e "${BLUE}=== Testing Security Headers ===${NC}"
    
    # Get response headers
    headers=$(curl -s -I "$API_ENDPOINT" 2>/dev/null || echo "")
    
    # Check for security headers
    if echo "$headers" | grep -i "X-Content-Type-Options" > /dev/null; then
        print_test_result "X-Content-Type-Options header" "PASS"
    else
        print_test_result "X-Content-Type-Options header" "WARNING" "Missing X-Content-Type-Options header"
    fi
    
    if echo "$headers" | grep -i "X-Frame-Options" > /dev/null; then
        print_test_result "X-Frame-Options header" "PASS"
    else
        print_test_result "X-Frame-Options header" "WARNING" "Missing X-Frame-Options header"
    fi
    
    if echo "$headers" | grep -i "X-XSS-Protection" > /dev/null; then
        print_test_result "X-XSS-Protection header" "PASS"
    else
        print_test_result "X-XSS-Protection header" "WARNING" "Missing X-XSS-Protection header"
    fi
    
    if echo "$headers" | grep -i "Strict-Transport-Security" > /dev/null; then
        print_test_result "HSTS header" "PASS"
    else
        print_test_result "HSTS header" "WARNING" "Missing HSTS header (expected for HTTPS)"
    fi
    
    # Check for information disclosure
    if echo "$headers" | grep -i "Server:" > /dev/null; then
        server_header=$(echo "$headers" | grep -i "Server:" | head -1)
        print_test_result "Server header disclosure" "WARNING" "Server header present: $server_header"
    else
        print_test_result "Server header disclosure" "PASS"
    fi
}

# Function to test input validation bypass attempts
test_input_validation() {
    echo -e "${BLUE}=== Testing Input Validation ===${NC}"
    
    # Test extremely long inputs
    long_string=$(printf 'A%.0s' {1..10000})
    response=$(curl -s -w "%{http_code}" -X POST \
        -H "Content-Type: application/json" \
        -d "{
            \"version\": \"$long_string\",
            \"name\": \"Test\",
            \"stepId\": \"123e4567-e89b-12d3-a456-426614174001\",
            \"entityIds\": [\"123e4567-e89b-12d3-a456-426614174002\"]
        }" \
        "$API_ENDPOINT" 2>/dev/null || echo "000")
    
    status_code="${response: -3}"
    if [ "$status_code" = "400" ]; then
        print_test_result "Long input validation" "PASS" "Properly rejected long input"
    elif [ "$status_code" = "500" ]; then
        print_test_result "Long input validation" "FAIL" "Server error on long input"
    else
        print_test_result "Long input validation" "WARNING" "Unexpected response to long input"
    fi
    
    # Test null byte injection
    response=$(curl -s -w "%{http_code}" -X POST \
        -H "Content-Type: application/json" \
        -d "{
            \"version\": \"1.0.0\",
            \"name\": \"Test\u0000Null\",
            \"stepId\": \"123e4567-e89b-12d3-a456-426614174001\",
            \"entityIds\": [\"123e4567-e89b-12d3-a456-426614174002\"]
        }" \
        "$API_ENDPOINT" 2>/dev/null || echo "000")
    
    status_code="${response: -3}"
    if [ "$status_code" = "400" ]; then
        print_test_result "Null byte injection" "PASS" "Properly rejected null byte"
    else
        print_test_result "Null byte injection" "WARNING" "Null byte not explicitly rejected"
    fi
}

# Function to test HTTP method security
test_http_methods() {
    echo -e "${BLUE}=== Testing HTTP Method Security ===${NC}"
    
    # Test unsupported methods
    local methods=("TRACE" "CONNECT" "HEAD" "OPTIONS")
    
    for method in "${methods[@]}"; do
        response=$(curl -s -w "%{http_code}" -X "$method" "$API_ENDPOINT" 2>/dev/null || echo "000")
        status_code="${response: -3}"
        
        case "$method" in
            "TRACE")
                if [ "$status_code" = "405" ] || [ "$status_code" = "501" ]; then
                    print_test_result "TRACE method disabled" "PASS"
                else
                    print_test_result "TRACE method disabled" "WARNING" "TRACE method may be enabled"
                fi
                ;;
            "CONNECT")
                if [ "$status_code" = "405" ] || [ "$status_code" = "501" ]; then
                    print_test_result "CONNECT method disabled" "PASS"
                else
                    print_test_result "CONNECT method disabled" "WARNING" "CONNECT method may be enabled"
                fi
                ;;
            "HEAD"|"OPTIONS")
                # These are typically allowed
                print_test_result "$method method response" "PASS" "Status: $status_code"
                ;;
        esac
    done
}

# Function to test rate limiting
test_rate_limiting() {
    echo -e "${BLUE}=== Testing Rate Limiting ===${NC}"
    
    # Send multiple rapid requests
    local rapid_requests=20
    local rate_limited=false
    
    for i in $(seq 1 $rapid_requests); do
        response=$(curl -s -w "%{http_code}" "$API_ENDPOINT" 2>/dev/null || echo "000")
        status_code="${response: -3}"
        
        if [ "$status_code" = "429" ]; then
            rate_limited=true
            break
        fi
        
        # Small delay to avoid overwhelming the server
        sleep 0.1
    done
    
    if [ "$rate_limited" = true ]; then
        print_test_result "Rate limiting" "PASS" "Rate limiting is active"
    else
        print_test_result "Rate limiting" "WARNING" "No rate limiting detected"
    fi
}

# Function to wait for API
wait_for_api() {
    echo -e "${BLUE}Waiting for API to be ready...${NC}"
    local max_attempts=30
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        if curl -s -f "$API_BASE_URL/health" > /dev/null 2>&1; then
            echo -e "${GREEN}API is ready!${NC}"
            return 0
        fi
        echo "Attempt $attempt/$max_attempts - API not ready yet..."
        sleep 2
        attempt=$((attempt + 1))
    done
    
    echo -e "${RED}API failed to become ready after $max_attempts attempts${NC}"
    exit 1
}

echo -e "${BLUE}=== AssignmentsController Security Test Suite ===${NC}"
echo -e "${BLUE}Testing API at: $API_BASE_URL${NC}"
echo -e "${YELLOW}Note: This is a basic security test. Professional security testing requires specialized tools.${NC}"
echo ""

# Wait for API to be ready
wait_for_api

echo -e "${BLUE}Starting security tests...${NC}"
echo ""

# Run security tests
test_sql_injection
test_xss_attacks
test_security_headers
test_input_validation
test_http_methods
test_rate_limiting

echo ""
echo -e "${BLUE}=== Security Test Summary ===${NC}"
echo -e "Total Tests: $TOTAL_TESTS"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${YELLOW}Warnings: $WARNINGS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"

echo ""
echo -e "${BLUE}=== Security Recommendations ===${NC}"

if [ $WARNINGS -gt 0 ]; then
    echo -e "${YELLOW}⚠ Security improvements recommended:${NC}"
    echo -e "  • Add security headers (X-Content-Type-Options, X-Frame-Options, etc.)"
    echo -e "  • Implement rate limiting"
    echo -e "  • Hide server information in headers"
    echo -e "  • Consider implementing HTTPS with HSTS"
fi

if [ $FAILED_TESTS -gt 0 ]; then
    echo -e "${RED}🚨 Critical security issues found:${NC}"
    echo -e "  • Review input validation and sanitization"
    echo -e "  • Check for SQL injection vulnerabilities"
    echo -e "  • Implement proper error handling"
fi

if [ $FAILED_TESTS -eq 0 ] && [ $WARNINGS -eq 0 ]; then
    echo -e "${GREEN}🛡️ Good security posture detected!${NC}"
fi

if [ $FAILED_TESTS -eq 0 ]; then
    exit 0
else
    exit 1
fi
