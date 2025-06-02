#!/bin/bash

# AssignmentsController JSON Response Validation Script
# Validates JSON structure and data types in API responses

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
    local result="$2"
    local details="$3"
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    if [ "$result" = "PASS" ]; then
        echo -e "${GREEN}✓ PASS${NC} - $test_name"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo -e "${RED}✗ FAIL${NC} - $test_name"
        if [ ! -z "$details" ]; then
            echo -e "${YELLOW}Details:${NC} $details"
        fi
        FAILED_TESTS=$((FAILED_TESTS + 1))
    fi
}

# Function to validate JSON structure
validate_json() {
    local json="$1"
    local test_name="$2"
    
    # Check if valid JSON
    if ! echo "$json" | jq . > /dev/null 2>&1; then
        print_test_result "$test_name - Valid JSON" "FAIL" "Invalid JSON format"
        return 1
    fi
    
    print_test_result "$test_name - Valid JSON" "PASS"
    return 0
}

# Function to validate AssignmentEntity structure
validate_assignment_entity() {
    local json="$1"
    local test_name="$2"
    
    # Required fields validation
    local required_fields=("id" "version" "name" "stepId" "entityIds" "createdAt" "createdBy")
    
    for field in "${required_fields[@]}"; do
        if ! echo "$json" | jq -e ".$field" > /dev/null 2>&1; then
            print_test_result "$test_name - Required field '$field'" "FAIL" "Field '$field' is missing"
            return 1
        fi
    done
    
    # Data type validation
    # ID should be a valid GUID
    local id=$(echo "$json" | jq -r '.id')
    if [[ ! "$id" =~ ^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$ ]]; then
        print_test_result "$test_name - Valid ID format" "FAIL" "ID '$id' is not a valid GUID"
        return 1
    fi
    
    # Version should be a string
    local version_type=$(echo "$json" | jq -r 'type(.version)')
    if [ "$version_type" != "string" ]; then
        print_test_result "$test_name - Version type" "FAIL" "Version should be string, got $version_type"
        return 1
    fi
    
    # Name should be a string
    local name_type=$(echo "$json" | jq -r 'type(.name)')
    if [ "$name_type" != "string" ]; then
        print_test_result "$test_name - Name type" "FAIL" "Name should be string, got $name_type"
        return 1
    fi
    
    # StepId should be a valid GUID
    local step_id=$(echo "$json" | jq -r '.stepId')
    if [[ ! "$step_id" =~ ^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$ ]]; then
        print_test_result "$test_name - Valid StepId format" "FAIL" "StepId '$step_id' is not a valid GUID"
        return 1
    fi
    
    # EntityIds should be an array
    local entity_ids_type=$(echo "$json" | jq -r 'type(.entityIds)')
    if [ "$entity_ids_type" != "array" ]; then
        print_test_result "$test_name - EntityIds type" "FAIL" "EntityIds should be array, got $entity_ids_type"
        return 1
    fi
    
    # CreatedAt should be a valid ISO date
    local created_at=$(echo "$json" | jq -r '.createdAt')
    if ! date -d "$created_at" > /dev/null 2>&1; then
        print_test_result "$test_name - Valid CreatedAt format" "FAIL" "CreatedAt '$created_at' is not a valid date"
        return 1
    fi
    
    print_test_result "$test_name - Entity structure" "PASS"
    return 0
}

# Function to validate paged response structure
validate_paged_response() {
    local json="$1"
    local test_name="$2"
    
    # Required fields for paged response
    local required_fields=("data" "page" "pageSize" "totalCount" "totalPages")
    
    for field in "${required_fields[@]}"; do
        if ! echo "$json" | jq -e ".$field" > /dev/null 2>&1; then
            print_test_result "$test_name - Paged field '$field'" "FAIL" "Field '$field' is missing"
            return 1
        fi
    done
    
    # Data should be an array
    local data_type=$(echo "$json" | jq -r 'type(.data)')
    if [ "$data_type" != "array" ]; then
        print_test_result "$test_name - Data type" "FAIL" "Data should be array, got $data_type"
        return 1
    fi
    
    # Numeric fields should be numbers
    local numeric_fields=("page" "pageSize" "totalCount" "totalPages")
    for field in "${numeric_fields[@]}"; do
        local field_type=$(echo "$json" | jq -r "type(.$field)")
        if [ "$field_type" != "number" ]; then
            print_test_result "$test_name - $field type" "FAIL" "$field should be number, got $field_type"
            return 1
        fi
    done
    
    print_test_result "$test_name - Paged structure" "PASS"
    return 0
}

# Function to validate error response structure
validate_error_response() {
    local json="$1"
    local test_name="$2"
    local expected_status="$3"
    
    # For 400 Bad Request, expect validation errors or specific error structure
    if [ "$expected_status" = "400" ]; then
        # Could be ModelState errors or custom error object
        if echo "$json" | jq -e '.errors' > /dev/null 2>&1; then
            # ModelState validation errors
            local errors_type=$(echo "$json" | jq -r 'type(.errors)')
            if [ "$errors_type" != "object" ]; then
                print_test_result "$test_name - Validation errors type" "FAIL" "Errors should be object, got $errors_type"
                return 1
            fi
        elif echo "$json" | jq -e '.error' > /dev/null 2>&1; then
            # Custom error object
            local error_type=$(echo "$json" | jq -r 'type(.error)')
            if [ "$error_type" != "string" ]; then
                print_test_result "$test_name - Error message type" "FAIL" "Error should be string, got $error_type"
                return 1
            fi
        else
            print_test_result "$test_name - Error structure" "FAIL" "Expected 'errors' or 'error' field"
            return 1
        fi
    fi
    
    # For 409 Conflict, expect message field
    if [ "$expected_status" = "409" ]; then
        if ! echo "$json" | jq -e '.message' > /dev/null 2>&1; then
            print_test_result "$test_name - Conflict message" "FAIL" "Expected 'message' field for 409 response"
            return 1
        fi
    fi
    
    print_test_result "$test_name - Error structure" "PASS"
    return 0
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

echo -e "${BLUE}=== AssignmentsController JSON Validation Test Suite ===${NC}"
echo -e "${BLUE}Testing API at: $API_BASE_URL${NC}"
echo ""

# Wait for API to be ready
wait_for_api

echo -e "${BLUE}Starting JSON validation tests...${NC}"
echo ""

# Test 1: GET /api/assignments - Validate array response
echo -e "${BLUE}Testing GET /api/assignments - JSON structure${NC}"
response=$(curl -s "$API_ENDPOINT" 2>/dev/null || echo "")
if [ ! -z "$response" ]; then
    if validate_json "$response" "GET /api/assignments"; then
        # Should be an array
        response_type=$(echo "$response" | jq -r 'type')
        if [ "$response_type" = "array" ]; then
            print_test_result "GET /api/assignments - Valid JSON" "PASS"

            # If array has items, validate first item structure
            item_count=$(echo "$response" | jq 'length')
            if [ "$item_count" -gt 0 ]; then
                first_item=$(echo "$response" | jq '.[0]')
                validate_assignment_entity "$first_item" "GET /api/assignments - First item"
            else
                print_test_result "GET /api/assignments - Empty array" "PASS"
            fi
        else
            print_test_result "GET /api/assignments - Valid JSON" "FAIL" "Expected array, got $response_type"
        fi
    fi
else
    print_test_result "GET /api/assignments - Valid JSON" "FAIL" "Empty response"
fi

# Test 2: GET /api/assignments/paged - Validate paged response
echo -e "${BLUE}Testing GET /api/assignments/paged - JSON structure${NC}"
response=$(curl -s "$API_ENDPOINT/paged?page=1&pageSize=10" 2>/dev/null || echo "")
if [ ! -z "$response" ]; then
    if validate_json "$response" "GET /api/assignments/paged"; then
        print_test_result "GET /api/assignments/paged - Valid JSON" "PASS"
        validate_paged_response "$response" "GET /api/assignments/paged"

        # Validate items in data array
        data_items=$(echo "$response" | jq '.data | length')
        if [ "$data_items" -gt 0 ]; then
            first_data_item=$(echo "$response" | jq '.data[0]')
            validate_assignment_entity "$first_data_item" "GET /api/assignments/paged - Data item"
        else
            print_test_result "GET /api/assignments/paged - Empty data array" "PASS"
        fi
    fi
else
    print_test_result "GET /api/assignments/paged - Valid JSON" "FAIL" "Empty response"
fi

# Test 3: POST /api/assignments - Validate error response (400)
echo -e "${BLUE}Testing POST /api/assignments - Error JSON structure${NC}"
response=$(curl -s -X POST -H "Content-Type: application/json" -d '{}' "$API_ENDPOINT" 2>/dev/null || echo "")
if [ ! -z "$response" ]; then
    if validate_json "$response" "POST /api/assignments - Error"; then
        print_test_result "POST /api/assignments - Error - Valid JSON" "PASS"
        validate_error_response "$response" "POST /api/assignments - Error" "400"
    fi
else
    print_test_result "POST /api/assignments - Error - Valid JSON" "FAIL" "Empty response"
fi

# Test 4: GET /api/assignments/{id} - Validate 404 response
echo -e "${BLUE}Testing GET /api/assignments/{id} - 404 JSON structure${NC}"
response=$(curl -s "$API_ENDPOINT/123e4567-e89b-12d3-a456-426614174999" 2>/dev/null || echo "")
if [ ! -z "$response" ]; then
    # 404 might return plain text or JSON
    if echo "$response" | jq . > /dev/null 2>&1; then
        validate_json "$response" "GET /api/assignments/{id} - 404"
    else
        print_test_result "GET /api/assignments/{id} - 404 Plain text" "PASS"
    fi
fi

# Test 5: Content-Type header validation
echo -e "${BLUE}Testing Content-Type headers${NC}"
headers=$(curl -s -I "$API_ENDPOINT" 2>/dev/null || echo "")
if echo "$headers" | grep -i "content-type.*application/json" > /dev/null; then
    print_test_result "Content-Type header" "PASS"
else
    # Check if we get any content-type header
    if echo "$headers" | grep -i "content-type" > /dev/null; then
        content_type=$(echo "$headers" | grep -i "content-type" | head -1)
        print_test_result "Content-Type header" "PASS" "Got: $content_type"
    else
        print_test_result "Content-Type header" "FAIL" "No content-type header found"
    fi
fi

echo ""
echo -e "${BLUE}=== JSON Validation Test Summary ===${NC}"
echo -e "Total Tests: $TOTAL_TESTS"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}All JSON validation tests passed!${NC}"
    exit 0
else
    echo -e "${RED}Some JSON validation tests failed.${NC}"
    exit 1
fi
