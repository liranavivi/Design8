#!/bin/bash

# AssignmentsController Performance Testing Script
# Tests API performance and response times

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

# Performance thresholds (in seconds)
FAST_THRESHOLD=0.1
ACCEPTABLE_THRESHOLD=0.5
SLOW_THRESHOLD=1.0

# Test counter
TOTAL_TESTS=0
FAST_TESTS=0
ACCEPTABLE_TESTS=0
SLOW_TESTS=0
FAILED_TESTS=0

# Function to print performance results
print_performance_result() {
    local test_name="$1"
    local response_time="$2"
    local status_code="$3"
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    if [ "$status_code" -ge 400 ]; then
        echo -e "${RED}✗ FAIL${NC} - $test_name (Status: $status_code, Time: ${response_time}s)"
        FAILED_TESTS=$((FAILED_TESTS + 1))
        return
    fi
    
    # Performance classification (using awk instead of bc)
    if awk "BEGIN {exit !($response_time < $FAST_THRESHOLD)}"; then
        echo -e "${GREEN}⚡ FAST${NC} - $test_name (${response_time}s)"
        FAST_TESTS=$((FAST_TESTS + 1))
    elif awk "BEGIN {exit !($response_time < $ACCEPTABLE_THRESHOLD)}"; then
        echo -e "${YELLOW}✓ OK${NC} - $test_name (${response_time}s)"
        ACCEPTABLE_TESTS=$((ACCEPTABLE_TESTS + 1))
    elif awk "BEGIN {exit !($response_time < $SLOW_THRESHOLD)}"; then
        echo -e "${YELLOW}⚠ SLOW${NC} - $test_name (${response_time}s)"
        SLOW_TESTS=$((SLOW_TESTS + 1))
    else
        echo -e "${RED}🐌 VERY SLOW${NC} - $test_name (${response_time}s)"
        SLOW_TESTS=$((SLOW_TESTS + 1))
    fi
}

# Function to measure response time
measure_response_time() {
    local url="$1"
    local method="${2:-GET}"
    local data="${3:-}"
    local headers="${4:-}"
    
    local start_time=$(date +%s.%N)
    
    if [ "$method" = "GET" ]; then
        local response=$(curl -s -w "%{http_code}" "$url" 2>/dev/null || echo "000")
    else
        local response=$(curl -s -w "%{http_code}" -X "$method" $headers -d "$data" "$url" 2>/dev/null || echo "000")
    fi
    
    local end_time=$(date +%s.%N)
    local response_time=$(awk "BEGIN {printf \"%.3f\", $end_time - $start_time}")
    local status_code="${response: -3}"
    
    echo "$response_time $status_code"
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

echo -e "${BLUE}=== AssignmentsController Performance Test Suite ===${NC}"
echo -e "${BLUE}Testing API at: $API_BASE_URL${NC}"
echo -e "${BLUE}Performance Thresholds:${NC}"
echo -e "  ${GREEN}Fast: < ${FAST_THRESHOLD}s${NC}"
echo -e "  ${YELLOW}Acceptable: < ${ACCEPTABLE_THRESHOLD}s${NC}"
echo -e "  ${YELLOW}Slow: < ${SLOW_THRESHOLD}s${NC}"
echo -e "  ${RED}Very Slow: >= ${SLOW_THRESHOLD}s${NC}"
echo ""

# Wait for API to be ready
wait_for_api

echo -e "${BLUE}Starting performance tests...${NC}"
echo ""

# Test 1: GET /api/assignments
echo -e "${BLUE}Testing GET /api/assignments${NC}"
result=$(measure_response_time "$API_ENDPOINT")
response_time=$(echo $result | cut -d' ' -f1)
status_code=$(echo $result | cut -d' ' -f2)
print_performance_result "GET /api/assignments" "$response_time" "$status_code"

# Test 2: GET /api/assignments/paged
echo -e "${BLUE}Testing GET /api/assignments/paged${NC}"
result=$(measure_response_time "$API_ENDPOINT/paged?page=1&pageSize=10")
response_time=$(echo $result | cut -d' ' -f1)
status_code=$(echo $result | cut -d' ' -f2)
print_performance_result "GET /api/assignments/paged" "$response_time" "$status_code"

# Test 3: GET /api/assignments/{id} - Not Found (should be fast)
echo -e "${BLUE}Testing GET /api/assignments/{id} - Not Found${NC}"
result=$(measure_response_time "$API_ENDPOINT/123e4567-e89b-12d3-a456-426614174999")
response_time=$(echo $result | cut -d' ' -f1)
status_code=$(echo $result | cut -d' ' -f2)
print_performance_result "GET /api/assignments/{id} - Not Found" "$response_time" "$status_code"

# Test 4: GET /api/assignments/by-name/{name}
echo -e "${BLUE}Testing GET /api/assignments/by-name/{name}${NC}"
result=$(measure_response_time "$API_ENDPOINT/by-name/TestAssignment")
response_time=$(echo $result | cut -d' ' -f1)
status_code=$(echo $result | cut -d' ' -f2)
print_performance_result "GET /api/assignments/by-name/{name}" "$response_time" "$status_code"

# Test 5: GET /api/assignments/by-version/{version}
echo -e "${BLUE}Testing GET /api/assignments/by-version/{version}${NC}"
result=$(measure_response_time "$API_ENDPOINT/by-version/1.0.0")
response_time=$(echo $result | cut -d' ' -f1)
status_code=$(echo $result | cut -d' ' -f2)
print_performance_result "GET /api/assignments/by-version/{version}" "$response_time" "$status_code"

# Test 6: POST /api/assignments - Validation Error (should be fast)
echo -e "${BLUE}Testing POST /api/assignments - Validation Error${NC}"
result=$(measure_response_time "$API_ENDPOINT" "POST" '{}' '-H "Content-Type: application/json"')
response_time=$(echo $result | cut -d' ' -f1)
status_code=$(echo $result | cut -d' ' -f2)
print_performance_result "POST /api/assignments - Validation Error" "$response_time" "$status_code"

# Test 7: Health Check (should be very fast)
echo -e "${BLUE}Testing GET /health${NC}"
result=$(measure_response_time "$API_BASE_URL/health")
response_time=$(echo $result | cut -d' ' -f1)
status_code=$(echo $result | cut -d' ' -f2)
print_performance_result "GET /health" "$response_time" "$status_code"

# Load Testing - Multiple concurrent requests
echo ""
echo -e "${BLUE}=== Load Testing (10 concurrent requests) ===${NC}"

# Function to run concurrent requests
run_concurrent_test() {
    local url="$1"
    local test_name="$2"
    local concurrent_requests=10
    
    echo -e "${BLUE}Running $concurrent_requests concurrent requests to $test_name${NC}"
    
    local start_time=$(date +%s.%N)
    
    # Run concurrent requests in background
    for i in $(seq 1 $concurrent_requests); do
        curl -s "$url" > /dev/null 2>&1 &
    done
    
    # Wait for all background jobs to complete
    wait
    
    local end_time=$(date +%s.%N)
    local total_time=$(awk "BEGIN {printf \"%.3f\", $end_time - $start_time}")
    local avg_time=$(awk "BEGIN {printf \"%.3f\", $total_time / $concurrent_requests}")
    
    echo -e "${GREEN}Completed $concurrent_requests requests in ${total_time}s (avg: ${avg_time}s per request)${NC}"
}

# Run concurrent tests
run_concurrent_test "$API_ENDPOINT" "GET /api/assignments"
run_concurrent_test "$API_ENDPOINT/paged?page=1&pageSize=5" "GET /api/assignments/paged"
run_concurrent_test "$API_BASE_URL/health" "GET /health"

echo ""
echo -e "${BLUE}=== Performance Test Summary ===${NC}"
echo -e "Total Tests: $TOTAL_TESTS"
echo -e "${GREEN}Fast (< ${FAST_THRESHOLD}s): $FAST_TESTS${NC}"
echo -e "${YELLOW}Acceptable (< ${ACCEPTABLE_THRESHOLD}s): $ACCEPTABLE_TESTS${NC}"
echo -e "${YELLOW}Slow (< ${SLOW_THRESHOLD}s): $SLOW_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"

# Performance recommendations
echo ""
echo -e "${BLUE}=== Performance Recommendations ===${NC}"

if [ $SLOW_TESTS -gt 0 ]; then
    echo -e "${YELLOW}⚠ Some endpoints are responding slowly. Consider:${NC}"
    echo -e "  • Database query optimization"
    echo -e "  • Adding database indexes"
    echo -e "  • Implementing caching"
    echo -e "  • Connection pool tuning"
fi

if [ $FAST_TESTS -eq $TOTAL_TESTS ] && [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}🎉 Excellent! All endpoints are performing well.${NC}"
elif [ $ACCEPTABLE_TESTS -gt 0 ] && [ $SLOW_TESTS -eq 0 ] && [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}✓ Good performance overall.${NC}"
fi

if [ $FAILED_TESTS -eq 0 ]; then
    exit 0
else
    exit 1
fi
