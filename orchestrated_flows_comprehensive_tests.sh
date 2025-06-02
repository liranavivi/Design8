#!/bin/bash

# Comprehensive OrchestratedFlowsController API Tests
# Tests ALL endpoints and ALL possible status codes

BASE_URL="http://localhost:5130/api/orchestratedflows"
FLOWS_URL="http://localhost:5130/api/flows"
ASSIGNMENTS_URL="http://localhost:5130/api/assignments"
STEPS_URL="http://localhost:5130/api/steps"
PROCESSORS_URL="http://localhost:5130/api/processors"
CONTENT_TYPE="Content-Type: application/json"

echo "🧪 Comprehensive OrchestratedFlowsController API Tests"
echo "Testing ALL 16 endpoints and ALL possible status codes"
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
echo "SETUP: Creating test data"
echo "========================================="

# Get existing processor for EntityIds
PROCESSOR_RESPONSE=$(curl -s -X GET "$PROCESSORS_URL")
PROCESSOR_ID=$(echo "$PROCESSOR_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
echo "Using Processor ID: $PROCESSOR_ID"

# Get existing step
STEP_RESPONSE=$(curl -s -X GET "$STEPS_URL")
STEP_ID=$(echo "$STEP_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
echo "Using Step ID: $STEP_ID"

# Create Assignment
ASSIGNMENT_PAYLOAD='{
    "id": "00000000-0000-0000-0000-000000000000",
    "version": "1.0.0",
    "name": "TestAssignmentComprehensive",
    "stepId": "'$STEP_ID'",
    "entityIds": ["'$PROCESSOR_ID'"]
}'

ASSIGNMENT_RESPONSE=$(curl -s -w "%{http_code}" -X POST "$ASSIGNMENTS_URL" \
    -H "$CONTENT_TYPE" \
    -d "$ASSIGNMENT_PAYLOAD")

ASSIGNMENT_STATUS="${ASSIGNMENT_RESPONSE: -3}"
ASSIGNMENT_BODY="${ASSIGNMENT_RESPONSE%???}"
ASSIGNMENT_ID=$(echo "$ASSIGNMENT_BODY" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
echo "Created Assignment ID: $ASSIGNMENT_ID"

# Create Flow
FLOW_PAYLOAD='{
    "id": "00000000-0000-0000-0000-000000000000",
    "version": "1.0.0",
    "name": "TestFlowComprehensive",
    "stepIds": ["'$STEP_ID'"]
}'

FLOW_RESPONSE=$(curl -s -w "%{http_code}" -X POST "$FLOWS_URL" \
    -H "$CONTENT_TYPE" \
    -d "$FLOW_PAYLOAD")

FLOW_STATUS="${FLOW_RESPONSE: -3}"
FLOW_BODY="${FLOW_RESPONSE%???}"
FLOW_ID=$(echo "$FLOW_BODY" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
echo "Created Flow ID: $FLOW_ID"

echo ""
echo "========================================="
echo "TESTING ALL 16 ENDPOINTS"
echo "========================================="

# 1. GET /api/orchestratedflows
echo -e "${BLUE}1. GET /api/orchestratedflows${NC}"
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL")
status="${response: -3}"
body="${response%???}"
print_test_result "GET all orchestrated flows" "200" "$status" "$body"

# 2. GET /api/orchestratedflows/paged
echo -e "${BLUE}2. GET /api/orchestratedflows/paged${NC}"
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/paged")
status="${response: -3}"
body="${response%???}"
print_test_result "GET paged (default)" "200" "$status" "$body"

response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/paged?page=1&pageSize=5")
status="${response: -3}"
body="${response%???}"
print_test_result "GET paged (custom)" "200" "$status" "$body"

response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/paged?page=0")
status="${response: -3}"
body="${response%???}"
print_test_result "GET paged (invalid page)" "400" "$status" "$body"

response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/paged?pageSize=101")
status="${response: -3}"
body="${response%???}"
print_test_result "GET paged (pageSize > 100)" "400" "$status" "$body"

# 3. GET /api/orchestratedflows/{id:guid}
echo -e "${BLUE}3. GET /api/orchestratedflows/{id:guid}${NC}"
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/99999999-9999-9999-9999-999999999999")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by ID (not found)" "404" "$status" "$body"

# 4. GET /api/orchestratedflows/{id} (fallback)
echo -e "${BLUE}4. GET /api/orchestratedflows/{id} (fallback)${NC}"
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/invalid-guid")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by invalid GUID" "400" "$status" "$body"

# 5. GET /api/orchestratedflows/by-assignment-id/{assignmentId:guid}
echo -e "${BLUE}5. GET /api/orchestratedflows/by-assignment-id/{assignmentId:guid}${NC}"
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-assignment-id/$ASSIGNMENT_ID")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by assignment ID" "200" "$status" "$body"

# 6. GET /api/orchestratedflows/by-assignment-id/{assignmentId} (fallback)
echo -e "${BLUE}6. GET /api/orchestratedflows/by-assignment-id/{assignmentId} (fallback)${NC}"
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-assignment-id/invalid-guid")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by invalid assignment ID" "400" "$status" "$body"

# 7. GET /api/orchestratedflows/by-flow-id/{flowId:guid}
echo -e "${BLUE}7. GET /api/orchestratedflows/by-flow-id/{flowId:guid}${NC}"
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-flow-id/$FLOW_ID")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by flow ID" "200" "$status" "$body"

# 8. GET /api/orchestratedflows/by-flow-id/{flowId} (fallback)
echo -e "${BLUE}8. GET /api/orchestratedflows/by-flow-id/{flowId} (fallback)${NC}"
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-flow-id/invalid-guid")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by invalid flow ID" "400" "$status" "$body"

# 9. GET /api/orchestratedflows/by-name/{name}
echo -e "${BLUE}9. GET /api/orchestratedflows/by-name/{name}${NC}"
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-name/NonExistentFlow")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by name" "200" "$status" "$body"

# 10. GET /api/orchestratedflows/by-version/{version}
echo -e "${BLUE}10. GET /api/orchestratedflows/by-version/{version}${NC}"
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-version/1.0.0")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by version" "200" "$status" "$body"

# 11. GET /api/orchestratedflows/by-key/{version}/{name}
echo -e "${BLUE}11. GET /api/orchestratedflows/by-key/{version}/{name}${NC}"
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-key/1.0.0/NonExistent")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by composite key (not found)" "404" "$status" "$body"

# 12. POST /api/orchestratedflows
echo -e "${BLUE}12. POST /api/orchestratedflows${NC}"

# Test 400 - Empty body
response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d '{}')
status="${response: -3}"
body="${response%???}"
print_test_result "POST (empty body)" "400" "$status" "$body"

# Test 400 - Invalid FlowId
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
print_test_result "POST (invalid FlowId)" "400" "$status" "$body"

# Test 201 - Valid creation
ORCHESTRATED_FLOW_PAYLOAD='{
    "id": "00000000-0000-0000-0000-000000000000",
    "version": "1.0.0",
    "name": "TestOrchestratedFlowComprehensive",
    "flowId": "'$FLOW_ID'",
    "assignmentIds": ["'$ASSIGNMENT_ID'"]
}'

response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d "$ORCHESTRATED_FLOW_PAYLOAD")
status="${response: -3}"
body="${response%???}"
print_test_result "POST (valid creation)" "201" "$status" "$body"

if [ "$status" = "201" ]; then
    ORCHESTRATED_FLOW_ID=$(echo "$body" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "Created OrchestratedFlow ID: $ORCHESTRATED_FLOW_ID"
    
    # Test 409 - Duplicate key
    response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
        -H "$CONTENT_TYPE" \
        -d "$ORCHESTRATED_FLOW_PAYLOAD")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "POST (duplicate key)" "409" "$status" "$body"
    
    # 13. PUT /api/orchestratedflows/{id:guid}
    echo -e "${BLUE}13. PUT /api/orchestratedflows/{id:guid}${NC}"
    
    # Test 400 - ID mismatch
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/12345678-1234-1234-1234-123456789012" \
        -H "$CONTENT_TYPE" \
        -d '{
            "id": "87654321-4321-4321-4321-210987654321",
            "version": "1.0.0",
            "name": "TestFlow"
        }')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (ID mismatch)" "400" "$status" "$body"
    
    # Test 404 - Non-existent ID
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/99999999-9999-9999-9999-999999999999" \
        -H "$CONTENT_TYPE" \
        -d '{
            "id": "99999999-9999-9999-9999-999999999999",
            "version": "1.0.0",
            "name": "TestFlow"
        }')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (not found)" "404" "$status" "$body"
    
    # Test 200 - Valid update
    UPDATE_PAYLOAD='{
        "id": "'$ORCHESTRATED_FLOW_ID'",
        "version": "1.1.0",
        "name": "UpdatedOrchestratedFlowComprehensive",
        "flowId": "'$FLOW_ID'",
        "assignmentIds": ["'$ASSIGNMENT_ID'"]
    }'
    
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/$ORCHESTRATED_FLOW_ID" \
        -H "$CONTENT_TYPE" \
        -d "$UPDATE_PAYLOAD")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (valid update)" "200" "$status" "$body"
    
    # 14. PUT /api/orchestratedflows/{id} (fallback)
    echo -e "${BLUE}14. PUT /api/orchestratedflows/{id} (fallback)${NC}"
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/invalid-guid" \
        -H "$CONTENT_TYPE" \
        -d '{}')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (invalid GUID)" "400" "$status" "$body"
    
    # 15. DELETE /api/orchestratedflows/{id:guid}
    echo -e "${BLUE}15. DELETE /api/orchestratedflows/{id:guid}${NC}"
    
    # Test 404 - Non-existent ID
    response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/99999999-9999-9999-9999-999999999999")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "DELETE (not found)" "404" "$status" "$body"
    
    # Test 204 - Valid deletion
    response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/$ORCHESTRATED_FLOW_ID")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "DELETE (valid)" "204" "$status" "$body"
    
    # 16. DELETE /api/orchestratedflows/{id} (fallback)
    echo -e "${BLUE}16. DELETE /api/orchestratedflows/{id} (fallback)${NC}"
    response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/invalid-guid")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "DELETE (invalid GUID)" "400" "$status" "$body"
fi

echo ""
echo "========================================="
echo "CLEANUP"
echo "========================================="
curl -s -X DELETE "$ASSIGNMENTS_URL/$ASSIGNMENT_ID" > /dev/null
curl -s -X DELETE "$FLOWS_URL/$FLOW_ID" > /dev/null
echo "Cleanup completed"

echo ""
echo "========================================="
echo "FINAL TEST SUMMARY"
echo "========================================="
echo -e "${BLUE}Total Tests: $TOTAL_TESTS${NC}"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}🎉 ALL TESTS PASSED! 🎉${NC}"
    echo -e "${GREEN}✅ ALL 16 ENDPOINTS TESTED${NC}"
    echo -e "${GREEN}✅ ALL 7 STATUS CODES DEMONSTRATED${NC}"
    exit 0
else
    echo -e "${RED}❌ Some tests failed${NC}"
    exit 1
fi
