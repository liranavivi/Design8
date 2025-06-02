#!/bin/bash

# Fixed OrchestratedFlowsController API Tests
# Properly creates prerequisites and tests all status codes

BASE_URL="http://localhost:5130/api/orchestratedflows"
FLOWS_URL="http://localhost:5130/api/flows"
ASSIGNMENTS_URL="http://localhost:5130/api/assignments"
STEPS_URL="http://localhost:5130/api/steps"
CONTENT_TYPE="Content-Type: application/json"

echo "🧪 Fixed OrchestratedFlowsController API Tests"
echo "Base URL: $BASE_URL"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test counters
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Function to print test results
print_test_result() {
    local test_name="$1"
    local expected_status="$2"
    local actual_status="$3"
    local response_body="$4"
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    if [ "$actual_status" = "$expected_status" ]; then
        echo -e "${GREEN}✅ PASS${NC} - $test_name (Status: $actual_status)"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo -e "${RED}❌ FAIL${NC} - $test_name (Expected: $expected_status, Got: $actual_status)"
        echo -e "${YELLOW}Response: $response_body${NC}"
        FAILED_TESTS=$((FAILED_TESTS + 1))
    fi
}

echo "========================================="
echo "SETUP: Creating prerequisite entities"
echo "========================================="

# Get an existing step ID from the database
echo "Getting existing Step ID..."
STEP_RESPONSE=$(curl -s -X GET "$STEPS_URL")
STEP_ID=$(echo "$STEP_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

if [ -z "$STEP_ID" ]; then
    echo "No existing steps found. Creating a new step..."
    # Get existing processor ID for the step
    PROCESSOR_RESPONSE=$(curl -s -X GET "http://localhost:5130/api/processors")
    PROCESSOR_ID=$(echo "$PROCESSOR_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

    STEP_PAYLOAD='{
        "id": "00000000-0000-0000-0000-000000000000",
        "version": "1.0.0",
        "name": "TestStepForOrchestratedFlow",
        "entityId": "'$PROCESSOR_ID'",
        "nextStepIds": [],
        "description": "Test step for orchestrated flow testing"
    }'

    STEP_CREATE_RESPONSE=$(curl -s -X POST "$STEPS_URL" \
        -H "$CONTENT_TYPE" \
        -d "$STEP_PAYLOAD")

    STEP_ID=$(echo "$STEP_CREATE_RESPONSE" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
fi

echo "Using Step ID: $STEP_ID"

# Get existing processor ID for Assignment EntityIds
echo "Getting existing Processor ID for Assignment..."
PROCESSOR_RESPONSE=$(curl -s -X GET "http://localhost:5130/api/processors")
PROCESSOR_ID=$(echo "$PROCESSOR_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
echo "Using Processor ID: $PROCESSOR_ID"

# Create an Assignment entity
echo "Creating Assignment entity..."
ASSIGNMENT_PAYLOAD='{
    "id": "00000000-0000-0000-0000-000000000000",
    "version": "1.0.0",
    "name": "TestAssignmentForOrchestratedFlow",
    "stepId": "'$STEP_ID'",
    "entityIds": ["'$PROCESSOR_ID'"]
}'

ASSIGNMENT_RESPONSE=$(curl -s -w "%{http_code}" -X POST "$ASSIGNMENTS_URL" \
    -H "$CONTENT_TYPE" \
    -d "$ASSIGNMENT_PAYLOAD")

ASSIGNMENT_STATUS="${ASSIGNMENT_RESPONSE: -3}"
ASSIGNMENT_BODY="${ASSIGNMENT_RESPONSE%???}"

if [ "$ASSIGNMENT_STATUS" = "201" ]; then
    ASSIGNMENT_ID=$(echo "$ASSIGNMENT_BODY" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "Created Assignment with ID: $ASSIGNMENT_ID"
else
    echo "Failed to create Assignment. Status: $ASSIGNMENT_STATUS"
    echo "Response: $ASSIGNMENT_BODY"
    exit 1
fi

# Create a Flow entity
echo "Creating Flow entity..."
FLOW_PAYLOAD='{
    "id": "00000000-0000-0000-0000-000000000000",
    "version": "1.0.0",
    "name": "TestFlowForOrchestratedFlow",
    "description": "Test flow for orchestrated flow testing",
    "stepIds": ["'$STEP_ID'"],
    "tags": ["test"],
    "metadata": {}
}'

FLOW_RESPONSE=$(curl -s -w "%{http_code}" -X POST "$FLOWS_URL" \
    -H "$CONTENT_TYPE" \
    -d "$FLOW_PAYLOAD")

FLOW_STATUS="${FLOW_RESPONSE: -3}"
FLOW_BODY="${FLOW_RESPONSE%???}"

if [ "$FLOW_STATUS" = "201" ]; then
    FLOW_ID=$(echo "$FLOW_BODY" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "Created Flow with ID: $FLOW_ID"
else
    echo "Failed to create Flow. Status: $FLOW_STATUS"
    echo "Response: $FLOW_BODY"
    exit 1
fi

echo ""
echo "========================================="
echo "TESTING: All Status Codes"
echo "========================================="

# Test 200 OK - GET operations
echo -e "${BLUE}Testing 200 OK responses...${NC}"

response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/orchestratedflows" "200" "$status" "$body"

response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/paged")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/orchestratedflows/paged" "200" "$status" "$body"

response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-name/NonExistent")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/orchestratedflows/by-name/{name}" "200" "$status" "$body"

# Test 400 Bad Request - Various scenarios
echo -e "${BLUE}Testing 400 Bad Request responses...${NC}"

response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/paged?page=0")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/orchestratedflows/paged (invalid page)" "400" "$status" "$body"

response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/invalid-guid")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/orchestratedflows/{invalid-guid}" "400" "$status" "$body"

response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d '{}')
status="${response: -3}"
body="${response%???}"
print_test_result "POST /api/orchestratedflows (empty body)" "400" "$status" "$body"

response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d '{
        "version": "1.0.0",
        "name": "TestFlow",
        "flowId": "99999999-9999-9999-9999-999999999999",
        "assignmentIds": []
    }')
status="${response: -3}"
body="${response%???}"
print_test_result "POST /api/orchestratedflows (invalid FlowId)" "400" "$status" "$body"

# Test 404 Not Found
echo -e "${BLUE}Testing 404 Not Found responses...${NC}"

response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/99999999-9999-9999-9999-999999999999")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/orchestratedflows/{non-existent-id}" "404" "$status" "$body"

response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-key/99.99.99/NonExistent")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/orchestratedflows/by-key/{version}/{name}" "404" "$status" "$body"

# Test 201 Created - Valid creation
echo -e "${BLUE}Testing 201 Created response...${NC}"

ORCHESTRATED_FLOW_PAYLOAD='{
    "id": "00000000-0000-0000-0000-000000000000",
    "version": "1.0.0",
    "name": "TestOrchestratedFlow",
    "flowId": "'$FLOW_ID'",
    "assignmentIds": ["'$ASSIGNMENT_ID'"]
}'

response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d "$ORCHESTRATED_FLOW_PAYLOAD")
status="${response: -3}"
body="${response%???}"
print_test_result "POST /api/orchestratedflows (valid data)" "201" "$status" "$body"

if [ "$status" = "201" ]; then
    ORCHESTRATED_FLOW_ID=$(echo "$body" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "Created OrchestratedFlow with ID: $ORCHESTRATED_FLOW_ID"
    
    # Test 409 Conflict - Duplicate key
    echo -e "${BLUE}Testing 409 Conflict response...${NC}"
    
    response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
        -H "$CONTENT_TYPE" \
        -d "$ORCHESTRATED_FLOW_PAYLOAD")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "POST /api/orchestratedflows (duplicate key)" "409" "$status" "$body"
    
    # Test 200 OK - Successful update
    echo -e "${BLUE}Testing 200 OK update response...${NC}"
    
    UPDATE_PAYLOAD='{
        "id": "'$ORCHESTRATED_FLOW_ID'",
        "version": "1.1.0",
        "name": "UpdatedOrchestratedFlow",
        "flowId": "'$FLOW_ID'",
        "assignmentIds": ["'$ASSIGNMENT_ID'"]
    }'
    
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/$ORCHESTRATED_FLOW_ID" \
        -H "$CONTENT_TYPE" \
        -d "$UPDATE_PAYLOAD")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT /api/orchestratedflows/{id} (valid update)" "200" "$status" "$body"
    
    # Test 204 No Content - Successful deletion
    echo -e "${BLUE}Testing 204 No Content response...${NC}"
    
    response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/$ORCHESTRATED_FLOW_ID")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "DELETE /api/orchestratedflows/{id}" "204" "$status" "$body"
fi

echo ""
echo "========================================="
echo "CLEANUP: Removing test entities"
echo "========================================="

echo "Cleaning up test entities..."
curl -s -X DELETE "$ASSIGNMENTS_URL/$ASSIGNMENT_ID" > /dev/null
curl -s -X DELETE "$FLOWS_URL/$FLOW_ID" > /dev/null
echo "Cleanup completed"

echo ""
echo "========================================="
echo "TEST SUMMARY"
echo "========================================="
echo -e "${BLUE}Total Tests: $TOTAL_TESTS${NC}"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}🎉 ALL TESTS PASSED! 🎉${NC}"
    exit 0
else
    echo -e "${RED}❌ Some tests failed${NC}"
    exit 1
fi
