#!/bin/bash

# AssignmentsController Edge Cases and Error Scenarios Test Script
# Tests specific error conditions and edge cases for comprehensive coverage

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

# Function to print test results
print_test_result() {
    local test_name="$1"
    local expected_status="$2"
    local actual_status="$3"
    local response="$4"
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    if [ "$expected_status" = "$actual_status" ]; then
        echo -e "${GREEN}✓ PASS${NC} - $test_name (Expected: $expected_status, Got: $actual_status)"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo -e "${RED}✗ FAIL${NC} - $test_name (Expected: $expected_status, Got: $actual_status)"
        echo -e "${YELLOW}Response:${NC} $response"
        FAILED_TESTS=$((FAILED_TESTS + 1))
    fi
}

echo -e "${BLUE}=== AssignmentsController Edge Cases Test Suite ===${NC}"
echo -e "${BLUE}Testing API at: $API_BASE_URL${NC}"
echo ""

# Wait for API to be ready
echo -e "${BLUE}Waiting for API to be ready...${NC}"
max_attempts=30
attempt=1

while [ $attempt -le $max_attempts ]; do
    if curl -s -f "$API_BASE_URL/health" > /dev/null 2>&1; then
        echo -e "${GREEN}API is ready!${NC}"
        break
    fi
    echo "Attempt $attempt/$max_attempts - API not ready yet..."
    sleep 2
    attempt=$((attempt + 1))
    
    if [ $attempt -gt $max_attempts ]; then
        echo -e "${RED}API failed to become ready after $max_attempts attempts${NC}"
        exit 1
    fi
done

echo -e "${BLUE}Starting edge case tests...${NC}"
echo ""

# === VALIDATION EDGE CASES ===
echo -e "${BLUE}=== Testing Validation Edge Cases ===${NC}"

# Test 1: POST with null values
response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d '{
        "version": null,
        "name": null,
        "stepId": null,
        "entityIds": null
    }' \
    "$API_ENDPOINT" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "POST - Null values" "400" "$status_code" "$response_body"

# Test 2: POST with empty strings
response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d '{
        "version": "",
        "name": "",
        "stepId": "00000000-0000-0000-0000-000000000000",
        "entityIds": []
    }' \
    "$API_ENDPOINT" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "POST - Empty strings" "400" "$status_code" "$response_body"

# Test 3: POST with version too long (>50 characters)
response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d '{
        "version": "1234567890123456789012345678901234567890123456789012345678901234567890",
        "name": "Test Assignment",
        "stepId": "123e4567-e89b-12d3-a456-426614174001",
        "entityIds": ["123e4567-e89b-12d3-a456-426614174002"]
    }' \
    "$API_ENDPOINT" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "POST - Version too long" "400" "$status_code" "$response_body"

# Test 4: POST with name too long (>200 characters)
response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d '{
        "version": "1.0.0",
        "name": "This is a very long name that exceeds the maximum allowed length of 200 characters. Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.",
        "stepId": "123e4567-e89b-12d3-a456-426614174001",
        "entityIds": ["123e4567-e89b-12d3-a456-426614174002"]
    }' \
    "$API_ENDPOINT" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "POST - Name too long" "400" "$status_code" "$response_body"

# Test 5: POST with invalid GUID format
response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d '{
        "version": "1.0.0",
        "name": "Test Assignment",
        "stepId": "invalid-guid-format",
        "entityIds": ["123e4567-e89b-12d3-a456-426614174002"]
    }' \
    "$API_ENDPOINT" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "POST - Invalid GUID format" "400" "$status_code" "$response_body"

# === CONTENT TYPE EDGE CASES ===
echo -e "${BLUE}=== Testing Content Type Edge Cases ===${NC}"

# Test 6: POST with wrong content type
response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: text/plain" \
    -d '{
        "version": "1.0.0",
        "name": "Test Assignment",
        "stepId": "123e4567-e89b-12d3-a456-426614174001",
        "entityIds": ["123e4567-e89b-12d3-a456-426614174002"]
    }' \
    "$API_ENDPOINT" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "POST - Wrong content type" "400" "$status_code" "$response_body"

# Test 7: POST with malformed JSON
response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d '{
        "version": "1.0.0",
        "name": "Test Assignment",
        "stepId": "123e4567-e89b-12d3-a456-426614174001",
        "entityIds": ["123e4567-e89b-12d3-a456-426614174002"
    }' \
    "$API_ENDPOINT" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "POST - Malformed JSON" "400" "$status_code" "$response_body"

# === HTTP METHOD EDGE CASES ===
echo -e "${BLUE}=== Testing HTTP Method Edge Cases ===${NC}"

# Test 8: PATCH method (not supported)
response=$(curl -s -w "%{http_code}" -X PATCH \
    -H "Content-Type: application/json" \
    -d '{}' \
    "$API_ENDPOINT/123e4567-e89b-12d3-a456-426614174001" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "PATCH - Method not allowed" "405" "$status_code" "$response_body"

# Test 9: OPTIONS method
response=$(curl -s -w "%{http_code}" -X OPTIONS \
    "$API_ENDPOINT" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "OPTIONS - Method check" "200" "$status_code" "$response_body"

# === PAGINATION EDGE CASES ===
echo -e "${BLUE}=== Testing Pagination Edge Cases ===${NC}"

# Test 10: Negative page number
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/paged?page=-1&pageSize=10" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET paged - Negative page" "400" "$status_code" "$response_body"

# Test 11: Zero page size
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/paged?page=1&pageSize=0" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET paged - Zero page size" "400" "$status_code" "$response_body"

# Test 12: Very large page size
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/paged?page=1&pageSize=1000" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET paged - Large page size" "400" "$status_code" "$response_body"

# Test 13: Non-numeric page parameters
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/paged?page=abc&pageSize=def" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET paged - Non-numeric parameters" "400" "$status_code" "$response_body"

# === URL ENCODING EDGE CASES ===
echo -e "${BLUE}=== Testing URL Encoding Edge Cases ===${NC}"

# Test 14: Special characters in name parameter
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/by-name/Test%20Assignment%20%26%20More" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET by-name - Special characters" "200" "$status_code" "$response_body"

# Test 15: Unicode characters in name parameter
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/by-name/Test%E2%9C%93Assignment" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET by-name - Unicode characters" "200" "$status_code" "$response_body"

# === LARGE PAYLOAD EDGE CASES ===
echo -e "${BLUE}=== Testing Large Payload Edge Cases ===${NC}"

# Test 16: Large EntityIds array
large_entity_ids=""
for i in {1..100}; do
    if [ $i -eq 1 ]; then
        large_entity_ids="\"$(printf "%08d-0000-0000-0000-000000000000" $i)\""
    else
        large_entity_ids="$large_entity_ids, \"$(printf "%08d-0000-0000-0000-000000000000" $i)\""
    fi
done

response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d "{
        \"version\": \"1.0.0\",
        \"name\": \"Large EntityIds Test\",
        \"stepId\": \"123e4567-e89b-12d3-a456-426614174001\",
        \"entityIds\": [$large_entity_ids]
    }" \
    "$API_ENDPOINT" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "POST - Large EntityIds array" "400" "$status_code" "$response_body"

echo ""
echo -e "${BLUE}=== Edge Cases Test Summary ===${NC}"
echo -e "Total Tests: $TOTAL_TESTS"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}All edge case tests passed!${NC}"
    exit 0
else
    echo -e "${RED}Some edge case tests failed.${NC}"
    exit 1
fi
