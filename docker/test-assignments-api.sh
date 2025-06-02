#!/bin/bash

# AssignmentsController API Test Script
# Tests all endpoints and their possible status codes using Docker

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

# Function to wait for API to be ready
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

# Function to create test data
create_test_data() {
    echo -e "${BLUE}Creating test data...${NC}"
    
    # Create a test StepEntity first (required for foreign key)
    # Note: This assumes Steps endpoint exists and works
    local step_response=$(curl -s -w "%{http_code}" -X POST \
        -H "Content-Type: application/json" \
        -d '{
            "version": "1.0.0",
            "name": "Test Step for Assignments",
            "entityId": "123e4567-e89b-12d3-a456-426614174000",
            "nextStepIds": [],
            "description": "Test step created for assignment testing"
        }' \
        "$API_BASE_URL/api/steps" 2>/dev/null || echo "000")
    
    local step_status="${step_response: -3}"
    local step_body="${step_response%???}"

    if [ "$step_status" = "201" ]; then
        echo -e "${GREEN}Test step created successfully${NC}"
        # Extract step ID from response
        TEST_STEP_ID=$(echo "$step_body" | grep -o '"id":"[^"]*"' | cut -d'"' -f4 || echo "")
        if [ -z "$TEST_STEP_ID" ]; then
            TEST_STEP_ID="123e4567-e89b-12d3-a456-426614174001"
        fi
    else
        echo -e "${YELLOW}Warning: Could not create test step (Status: $step_status). Creating directly in database...${NC}"

        # Try to create step directly via MongoDB (fallback)
        TEST_STEP_ID="123e4567-e89b-12d3-a456-426614174001"

        # Create step directly in MongoDB using mongosh
        local mongo_result=$(docker exec entitiesmanager-mongodb mongosh --quiet --eval "
            db = db.getSiblingDB('entitiesmanager');
            result = db.steps.insertOne({
                _id: ObjectId(),
                id: UUID('$TEST_STEP_ID'),
                version: '1.0.0',
                name: 'Test Step for Assignments',
                entityId: UUID('123e4567-e89b-12d3-a456-426614174000'),
                nextStepIds: [],
                description: 'Test step created for assignment testing',
                createdAt: new Date(),
                createdBy: 'test-system',
                updatedAt: new Date(),
                updatedBy: 'test-system'
            });
            print(result.acknowledged ? 'SUCCESS' : 'FAILED');
        " 2>/dev/null || echo "FAILED")

        if [[ "$mongo_result" == *"SUCCESS"* ]]; then
            echo -e "${GREEN}Test step created directly in database${NC}"
        else
            echo -e "${YELLOW}Warning: Could not create test step. Using hardcoded GUID.${NC}"
        fi
    fi
    
    # Create a valid assignment for testing
    local assignment_response=$(curl -s -w "%{http_code}" -X POST \
        -H "Content-Type: application/json" \
        -d "{
            \"version\": \"1.0.0\",
            \"name\": \"Test Assignment\",
            \"stepId\": \"$TEST_STEP_ID\",
            \"entityIds\": [\"123e4567-e89b-12d3-a456-426614174002\"]
        }" \
        "$API_ENDPOINT" 2>/dev/null || echo "000")
    
    local assignment_status="${assignment_response: -3}"
    local assignment_body="${assignment_response%???}"

    if [ "$assignment_status" = "201" ]; then
        echo -e "${GREEN}Test assignment created successfully${NC}"
        # Extract assignment ID from response
        TEST_ASSIGNMENT_ID=$(echo "$assignment_body" | grep -o '"id":"[^"]*"' | cut -d'"' -f4 || echo "123e4567-e89b-12d3-a456-426614174003")
    else
        echo -e "${YELLOW}Warning: Could not create test assignment (Status: $assignment_status). Using hardcoded GUID.${NC}"
        TEST_ASSIGNMENT_ID="123e4567-e89b-12d3-a456-426614174003"
    fi
}

echo -e "${BLUE}=== AssignmentsController API Test Suite ===${NC}"
echo -e "${BLUE}Testing API at: $API_BASE_URL${NC}"
echo ""

# Wait for API to be ready
wait_for_api

# Create test data
create_test_data

echo -e "${BLUE}Starting API tests...${NC}"
echo ""

# Test 1: GET /api/assignments - GetAll() - 200 OK
echo -e "${BLUE}Testing GET /api/assignments (GetAll)${NC}"
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET /api/assignments - Success" "200" "$status_code" "$response_body"

# Test 2: GET /api/assignments/paged - GetPaged() - 200 OK
echo -e "${BLUE}Testing GET /api/assignments/paged (GetPaged)${NC}"
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/paged?page=1&pageSize=10" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET /api/assignments/paged - Success" "200" "$status_code" "$response_body"

# Test 3: GET /api/assignments/paged - GetPaged() - 400 Bad Request (invalid page)
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/paged?page=0&pageSize=10" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET /api/assignments/paged - Invalid page" "400" "$status_code" "$response_body"

# Test 4: GET /api/assignments/paged - GetPaged() - 400 Bad Request (invalid pageSize)
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/paged?page=1&pageSize=101" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET /api/assignments/paged - Invalid pageSize" "400" "$status_code" "$response_body"

# Test 5: GET /api/assignments/{id} - GetById() - 404 Not Found
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/123e4567-e89b-12d3-a456-426614174999" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET /api/assignments/{id} - Not Found" "404" "$status_code" "$response_body"

# Test 6: GET /api/assignments/{id} - GetById() - 200 OK (if test assignment exists)
if [ ! -z "$TEST_ASSIGNMENT_ID" ]; then
    response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/$TEST_ASSIGNMENT_ID" 2>/dev/null || echo "000")
    status_code="${response: -3}"
    response_body="${response%???}"
    print_test_result "GET /api/assignments/{id} - Success" "200" "$status_code" "$response_body"
fi

# Test 7: GET /api/assignments/by-key/{stepId} - GetByCompositeKey() - 404 Not Found
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/by-key/123e4567-e89b-12d3-a456-426614174999" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET /api/assignments/by-key/{stepId} - Not Found" "404" "$status_code" "$response_body"

# Test 8: GET /api/assignments/by-step/{stepId} - GetByStepId() - 404 Not Found
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/by-step/123e4567-e89b-12d3-a456-426614174999" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET /api/assignments/by-step/{stepId} - Not Found" "404" "$status_code" "$response_body"

# Test 9: GET /api/assignments/by-entity/{entityId} - GetByEntityId() - 200 OK
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/by-entity/123e4567-e89b-12d3-a456-426614174999" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET /api/assignments/by-entity/{entityId} - Success" "200" "$status_code" "$response_body"

# Test 10: GET /api/assignments/by-name/{name} - GetByName() - 400 Bad Request (empty name)
# Note: Using URL encoded space to reach the controller with empty parameter
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/by-name/%20" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET /api/assignments/by-name/ - Empty name" "400" "$status_code" "$response_body"

# Test 11: GET /api/assignments/by-name/{name} - GetByName() - 200 OK
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/by-name/TestAssignment" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET /api/assignments/by-name/{name} - Success" "200" "$status_code" "$response_body"

# Test 12: GET /api/assignments/by-version/{version} - GetByVersion() - 400 Bad Request (empty version)
# Note: Using URL encoded space to reach the controller with empty parameter
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/by-version/%20" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET /api/assignments/by-version/ - Empty version" "400" "$status_code" "$response_body"

# Test 13: GET /api/assignments/by-version/{version} - GetByVersion() - 200 OK
response=$(curl -s -w "%{http_code}" "$API_ENDPOINT/by-version/1.0.0" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "GET /api/assignments/by-version/{version} - Success" "200" "$status_code" "$response_body"

# Test 14: POST /api/assignments - Create() - 400 Bad Request (missing required fields)
response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d '{}' \
    "$API_ENDPOINT" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "POST /api/assignments - Missing required fields" "400" "$status_code" "$response_body"

# Test 15: POST /api/assignments - Create() - 400 Bad Request (invalid StepId - foreign key validation)
response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d '{
        "version": "1.0.0",
        "name": "Test Assignment",
        "stepId": "123e4567-e89b-12d3-a456-426614174999",
        "entityIds": ["123e4567-e89b-12d3-a456-426614174002"]
    }' \
    "$API_ENDPOINT" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "POST /api/assignments - Foreign key validation failed" "400" "$status_code" "$response_body"

# Test 16: POST /api/assignments - Create() - 201 Created (valid data)
response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d "{
        \"version\": \"2.0.0\",
        \"name\": \"New Test Assignment\",
        \"stepId\": \"$TEST_STEP_ID\",
        \"entityIds\": [\"123e4567-e89b-12d3-a456-426614174004\"]
    }" \
    "$API_ENDPOINT" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "POST /api/assignments - Success" "201" "$status_code" "$response_body"

# Extract the created assignment ID for further tests
if [ "$status_code" = "201" ]; then
    CREATED_ASSIGNMENT_ID=$(echo "$response_body" | grep -o '"id":"[^"]*"' | cut -d'"' -f4 || echo "")
fi

# Test 17: POST /api/assignments - Create() - 409 Conflict (duplicate composite key)
response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d "{
        \"version\": \"2.0.0\",
        \"name\": \"Duplicate Assignment\",
        \"stepId\": \"$TEST_STEP_ID\",
        \"entityIds\": [\"123e4567-e89b-12d3-a456-426614174005\"]
    }" \
    "$API_ENDPOINT" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "POST /api/assignments - Duplicate key conflict" "409" "$status_code" "$response_body"

# Test 18: PUT /api/assignments/{id} - Update() - 400 Bad Request (ID mismatch)
if [ ! -z "$CREATED_ASSIGNMENT_ID" ]; then
    response=$(curl -s -w "%{http_code}" -X PUT \
        -H "Content-Type: application/json" \
        -d "{
            \"id\": \"123e4567-e89b-12d3-a456-426614174999\",
            \"version\": \"2.1.0\",
            \"name\": \"Updated Assignment\",
            \"stepId\": \"$TEST_STEP_ID\",
            \"entityIds\": [\"123e4567-e89b-12d3-a456-426614174004\"]
        }" \
        "$API_ENDPOINT/$CREATED_ASSIGNMENT_ID" 2>/dev/null || echo "000")
    status_code="${response: -3}"
    response_body="${response%???}"
    print_test_result "PUT /api/assignments/{id} - ID mismatch" "400" "$status_code" "$response_body"
fi

# Test 19: PUT /api/assignments/{id} - Update() - 404 Not Found
response=$(curl -s -w "%{http_code}" -X PUT \
    -H "Content-Type: application/json" \
    -d '{
        "id": "123e4567-e89b-12d3-a456-426614174999",
        "version": "2.1.0",
        "name": "Updated Assignment",
        "stepId": "123e4567-e89b-12d3-a456-426614174001",
        "entityIds": ["123e4567-e89b-12d3-a456-426614174004"]
    }' \
    "$API_ENDPOINT/123e4567-e89b-12d3-a456-426614174999" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "PUT /api/assignments/{id} - Not Found" "404" "$status_code" "$response_body"

# Test 20: PUT /api/assignments/{id} - Update() - 200 OK (valid update)
if [ ! -z "$CREATED_ASSIGNMENT_ID" ]; then
    response=$(curl -s -w "%{http_code}" -X PUT \
        -H "Content-Type: application/json" \
        -d "{
            \"id\": \"$CREATED_ASSIGNMENT_ID\",
            \"version\": \"2.1.0\",
            \"name\": \"Updated Assignment\",
            \"stepId\": \"$TEST_STEP_ID\",
            \"entityIds\": [\"123e4567-e89b-12d3-a456-426614174004\"]
        }" \
        "$API_ENDPOINT/$CREATED_ASSIGNMENT_ID" 2>/dev/null || echo "000")
    status_code="${response: -3}"
    response_body="${response%???}"
    print_test_result "PUT /api/assignments/{id} - Success" "200" "$status_code" "$response_body"
fi

# Test 21: DELETE /api/assignments/{id} - Delete() - 404 Not Found
response=$(curl -s -w "%{http_code}" -X DELETE \
    "$API_ENDPOINT/123e4567-e89b-12d3-a456-426614174999" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "DELETE /api/assignments/{id} - Not Found" "404" "$status_code" "$response_body"

# Test 22: DELETE /api/assignments/{id} - Delete() - 204 No Content (successful deletion)
if [ ! -z "$CREATED_ASSIGNMENT_ID" ]; then
    response=$(curl -s -w "%{http_code}" -X DELETE \
        "$API_ENDPOINT/$CREATED_ASSIGNMENT_ID" 2>/dev/null || echo "000")
    status_code="${response: -3}"
    response_body="${response%???}"
    print_test_result "DELETE /api/assignments/{id} - Success" "204" "$status_code" "$response_body"
fi

echo ""
echo -e "${BLUE}=== Test Summary ===${NC}"
echo -e "Total Tests: $TOTAL_TESTS"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}All tests passed!${NC}"
    exit 0
else
    echo -e "${RED}Some tests failed.${NC}"
    exit 1
fi
