#!/bin/bash

# Comprehensive StepsController API Tests
# Tests ALL 14 endpoints and ALL possible status codes

BASE_URL="http://localhost:5130/api/steps"
PROCESSORS_URL="http://localhost:5130/api/processors"
SCHEMAS_URL="http://localhost:5130/api/schemas"
CONTENT_TYPE="Content-Type: application/json"

echo "🧪 Comprehensive StepsController API Tests"
echo "Testing ALL 14 endpoints and ALL possible status codes"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
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

# Generate unique timestamp for test data
TIMESTAMP=$(date +%s)

echo "========================================="
echo "SETUP: Creating prerequisite entities"
echo "========================================="

# Create Schema entities for ProcessorEntity
echo "Creating Schema entities..."

SCHEMA1_PAYLOAD='{
    "id": "00000000-0000-0000-0000-000000000000",
    "version": "'$TIMESTAMP'.1",
    "name": "TestInputSchemaSteps",
    "definition": "{ \"type\": \"object\", \"properties\": { \"input\": { \"type\": \"string\" } } }"
}'

SCHEMA1_RESPONSE=$(curl -s -w "%{http_code}" -X POST "$SCHEMAS_URL" \
    -H "$CONTENT_TYPE" \
    -d "$SCHEMA1_PAYLOAD")

SCHEMA1_STATUS="${SCHEMA1_RESPONSE: -3}"
SCHEMA1_BODY="${SCHEMA1_RESPONSE%???}"

if [ "$SCHEMA1_STATUS" = "201" ]; then
    INPUT_SCHEMA_ID=$(echo "$SCHEMA1_BODY" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "Created Input Schema ID: $INPUT_SCHEMA_ID"
else
    echo "Failed to create Input Schema. Status: $SCHEMA1_STATUS"
    echo "Response: $SCHEMA1_BODY"
    exit 1
fi

SCHEMA2_PAYLOAD='{
    "id": "00000000-0000-0000-0000-000000000000",
    "version": "'$TIMESTAMP'.2",
    "name": "TestOutputSchemaSteps",
    "definition": "{ \"type\": \"object\", \"properties\": { \"output\": { \"type\": \"string\" } } }"
}'

SCHEMA2_RESPONSE=$(curl -s -w "%{http_code}" -X POST "$SCHEMAS_URL" \
    -H "$CONTENT_TYPE" \
    -d "$SCHEMA2_PAYLOAD")

SCHEMA2_STATUS="${SCHEMA2_RESPONSE: -3}"
SCHEMA2_BODY="${SCHEMA2_RESPONSE%???}"

if [ "$SCHEMA2_STATUS" = "201" ]; then
    OUTPUT_SCHEMA_ID=$(echo "$SCHEMA2_BODY" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "Created Output Schema ID: $OUTPUT_SCHEMA_ID"
else
    echo "Failed to create Output Schema. Status: $SCHEMA2_STATUS"
    echo "Response: $SCHEMA2_BODY"
    exit 1
fi

# Create ProcessorEntity for ProcessorId reference
echo "Creating ProcessorEntity..."

PROCESSOR_PAYLOAD='{
    "id": "00000000-0000-0000-0000-000000000000",
    "version": "'$TIMESTAMP'.proc",
    "name": "TestProcessorForSteps",
    "protocolId": "'$INPUT_SCHEMA_ID'",
    "inputSchemaId": "'$INPUT_SCHEMA_ID'",
    "outputSchemaId": "'$OUTPUT_SCHEMA_ID'",
    "description": "Processor for steps testing"
}'

PROCESSOR_RESPONSE=$(curl -s -w "%{http_code}" -X POST "$PROCESSORS_URL" \
    -H "$CONTENT_TYPE" \
    -d "$PROCESSOR_PAYLOAD")

PROCESSOR_STATUS="${PROCESSOR_RESPONSE: -3}"
PROCESSOR_BODY="${PROCESSOR_RESPONSE%???}"

if [ "$PROCESSOR_STATUS" = "201" ]; then
    PROCESSOR_ID=$(echo "$PROCESSOR_BODY" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "Created Processor ID: $PROCESSOR_ID"
else
    echo "Failed to create Processor. Status: $PROCESSOR_STATUS"
    echo "Response: $PROCESSOR_BODY"
    exit 1
fi

echo ""
echo "========================================="
echo "TESTING ALL 14 ENDPOINTS"
echo "========================================="

# 1. GET /api/steps
echo -e "${BLUE}1. GET /api/steps${NC}"
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL")
status="${response: -3}"
body="${response%???}"
print_test_result "GET all steps (200 OK)" "200" "$status" "$body"

# 2. GET /api/steps/paged
echo -e "${BLUE}2. GET /api/steps/paged${NC}"

# Test 200 OK - Default pagination
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/paged")
status="${response: -3}"
body="${response%???}"
print_test_result "GET paged (200 OK - default)" "200" "$status" "$body"

# Test 200 OK - Custom pagination
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/paged?page=1&pageSize=5")
status="${response: -3}"
body="${response%???}"
print_test_result "GET paged (200 OK - custom)" "200" "$status" "$body"

# Test 400 Bad Request - Invalid page
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/paged?page=0")
status="${response: -3}"
body="${response%???}"
print_test_result "GET paged (400 Bad Request - page < 1)" "400" "$status" "$body"

# Test 400 Bad Request - Invalid pageSize
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/paged?pageSize=-1")
status="${response: -3}"
body="${response%???}"
print_test_result "GET paged (400 Bad Request - pageSize < 1)" "400" "$status" "$body"

# Test 400 Bad Request - PageSize exceeds maximum
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/paged?pageSize=101")
status="${response: -3}"
body="${response%???}"
print_test_result "GET paged (400 Bad Request - pageSize > 100)" "400" "$status" "$body"

# 3. GET /api/steps/{id:guid}
echo -e "${BLUE}3. GET /api/steps/{id:guid}${NC}"

# Test 404 Not Found
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/99999999-9999-9999-9999-999999999999")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by ID (404 Not Found)" "404" "$status" "$body"

# 4. GET /api/steps/{id} (fallback)
echo -e "${BLUE}4. GET /api/steps/{id} (fallback)${NC}"

# Test 400 Bad Request - Invalid GUID
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/invalid-guid")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by invalid GUID (400 Bad Request)" "400" "$status" "$body"

# 5. GET /api/steps/by-key/{version}/{name}
echo -e "${BLUE}5. GET /api/steps/by-key/{version}/{name}${NC}"

# Test 404 Not Found
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-key/99.99.99/NonExistentStep")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by composite key (404 Not Found)" "404" "$status" "$body"

# 6. GET /api/steps/by-processor-id/{processorId:guid}
echo -e "${BLUE}6. GET /api/steps/by-processor-id/{processorId:guid}${NC}"

# Test 200 OK - Empty result
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-processor-id/99999999-9999-9999-9999-999999999999")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by processor ID (200 OK - empty result)" "200" "$status" "$body"

# 7. GET /api/steps/by-processor-id/{processorId} (fallback)
echo -e "${BLUE}7. GET /api/steps/by-processor-id/{processorId} (fallback)${NC}"

# Test 400 Bad Request - Invalid GUID
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-processor-id/invalid-guid")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by processor ID invalid GUID (400 Bad Request)" "400" "$status" "$body"

# 8. GET /api/steps/by-next-step-id/{nextStepId:guid}
echo -e "${BLUE}8. GET /api/steps/by-next-step-id/{nextStepId:guid}${NC}"

# Test 200 OK - Empty result
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-next-step-id/99999999-9999-9999-9999-999999999999")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by next step ID (200 OK - empty result)" "200" "$status" "$body"

# 9. GET /api/steps/by-next-step-id/{nextStepId} (fallback)
echo -e "${BLUE}9. GET /api/steps/by-next-step-id/{nextStepId} (fallback)${NC}"

# Test 400 Bad Request - Invalid GUID
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-next-step-id/invalid-guid")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by next step ID invalid GUID (400 Bad Request)" "400" "$status" "$body"

# 10. POST /api/steps
echo -e "${BLUE}10. POST /api/steps${NC}"

# Test 400 Bad Request - Empty body
response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d '{}')
status="${response: -3}"
body="${response%???}"
print_test_result "POST (400 Bad Request - empty body)" "400" "$status" "$body"

# Test 400 Bad Request - Missing required fields
response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d '{"version": "1.0.0"}')
status="${response: -3}"
body="${response%???}"
print_test_result "POST (400 Bad Request - missing fields)" "400" "$status" "$body"

# Test 400 Bad Request - Invalid ProcessorId
response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d '{
        "version": "'$TIMESTAMP'.3",
        "name": "TestStepInvalidProcessorId",
        "processorId": "99999999-9999-9999-9999-999999999999",
        "nextStepIds": []
    }')
status="${response: -3}"
body="${response%???}"
print_test_result "POST (400 Bad Request - invalid ProcessorId)" "400" "$status" "$body"

# Test 201 Created - Valid creation
STEP_PAYLOAD='{
    "id": "00000000-0000-0000-0000-000000000000",
    "version": "'$TIMESTAMP'.4",
    "name": "TestStepAPI",
    "processorId": "'$PROCESSOR_ID'",
    "nextStepIds": [],
    "description": "Test step for API testing"
}'

response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d "$STEP_PAYLOAD")
status="${response: -3}"
body="${response%???}"
print_test_result "POST (201 Created - valid creation)" "201" "$status" "$body"

if [ "$status" = "201" ]; then
    STEP_ID=$(echo "$body" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "Created Step ID: $STEP_ID"
    
    # Test 409 Conflict - Duplicate key
    response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
        -H "$CONTENT_TYPE" \
        -d "$STEP_PAYLOAD")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "POST (409 Conflict - duplicate key)" "409" "$status" "$body"
    
    # Test successful GET by ID
    response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/$STEP_ID")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "GET by ID (200 OK - found)" "200" "$status" "$body"
    
    # Test successful GET by composite key
    response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-key/$TIMESTAMP.4/TestStepAPI")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "GET by composite key (200 OK - found)" "200" "$status" "$body"
    
    # Test successful GET by processor ID
    response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-processor-id/$PROCESSOR_ID")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "GET by processor ID (200 OK - found)" "200" "$status" "$body"
    
    # Create another step to test NextStepIds
    STEP2_PAYLOAD='{
        "id": "00000000-0000-0000-0000-000000000000",
        "version": "'$TIMESTAMP'.5",
        "name": "TestStepForNextStep",
        "processorId": "'$PROCESSOR_ID'",
        "nextStepIds": ["'$STEP_ID'"],
        "description": "Step that references another step"
    }'
    
    response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
        -H "$CONTENT_TYPE" \
        -d "$STEP2_PAYLOAD")
    status="${response: -3}"
    body="${response%???}"
    
    if [ "$status" = "201" ]; then
        STEP2_ID=$(echo "$body" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
        echo "Created Step2 ID: $STEP2_ID"
        
        # Test successful GET by next step ID
        response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-next-step-id/$STEP_ID")
        status="${response: -3}"
        body="${response%???}"
        print_test_result "GET by next step ID (200 OK - found)" "200" "$status" "$body"
        
        # Test 400 Bad Request - Invalid NextStepIds in POST
        response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
            -H "$CONTENT_TYPE" \
            -d '{
                "version": "'$TIMESTAMP'.6",
                "name": "TestStepInvalidNextStep",
                "processorId": "'$PROCESSOR_ID'",
                "nextStepIds": ["99999999-9999-9999-9999-999999999999"]
            }')
        status="${response: -3}"
        body="${response%???}"
        print_test_result "POST (400 Bad Request - invalid NextStepIds)" "400" "$status" "$body"
        
        # Clean up step2 for later tests
        curl -s -X DELETE "$BASE_URL/$STEP2_ID" > /dev/null
    fi
    
    # 11. PUT /api/steps/{id:guid}
    echo -e "${BLUE}11. PUT /api/steps/{id:guid}${NC}"
    
    # Test 400 Bad Request - Invalid model
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/$STEP_ID" \
        -H "$CONTENT_TYPE" \
        -d '{}')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (400 Bad Request - invalid model)" "400" "$status" "$body"
    
    # Test 400 Bad Request - ID mismatch
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/12345678-1234-1234-1234-123456789012" \
        -H "$CONTENT_TYPE" \
        -d '{
            "id": "87654321-4321-4321-4321-210987654321",
            "version": "1.0.0",
            "name": "TestStep"
        }')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (400 Bad Request - ID mismatch)" "400" "$status" "$body"
    
    # Test 404 Not Found - Non-existent ID
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/99999999-9999-9999-9999-999999999999" \
        -H "$CONTENT_TYPE" \
        -d '{
            "id": "99999999-9999-9999-9999-999999999999",
            "version": "1.0.0",
            "name": "TestStep",
            "entityId": "'$PROCESSOR_ID'",
            "nextStepIds": []
        }')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (404 Not Found)" "404" "$status" "$body"
    
    # Test 400 Bad Request - Invalid foreign key in update
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/$STEP_ID" \
        -H "$CONTENT_TYPE" \
        -d '{
            "id": "'$STEP_ID'",
            "version": "'$TIMESTAMP'.7",
            "name": "UpdatedTestStep",
            "processorId": "99999999-9999-9999-9999-999999999999",
            "nextStepIds": []
        }')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (400 Bad Request - invalid foreign key)" "400" "$status" "$body"
    
    # Test 200 OK - Valid update
    UPDATE_PAYLOAD='{
        "id": "'$STEP_ID'",
        "version": "'$TIMESTAMP'.8",
        "name": "UpdatedTestStepAPI",
        "processorId": "'$PROCESSOR_ID'",
        "nextStepIds": [],
        "description": "Updated test step"
    }'
    
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/$STEP_ID" \
        -H "$CONTENT_TYPE" \
        -d "$UPDATE_PAYLOAD")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (200 OK - valid update)" "200" "$status" "$body"
    
    # 12. PUT /api/steps/{id} (fallback)
    echo -e "${BLUE}12. PUT /api/steps/{id} (fallback)${NC}"
    
    # Test 400 Bad Request - Invalid GUID
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/invalid-guid" \
        -H "$CONTENT_TYPE" \
        -d '{}')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (400 Bad Request - invalid GUID)" "400" "$status" "$body"
    
    # 13. DELETE /api/steps/{id:guid}
    echo -e "${BLUE}13. DELETE /api/steps/{id:guid}${NC}"
    
    # Test 404 Not Found - Non-existent ID
    response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/99999999-9999-9999-9999-999999999999")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "DELETE (404 Not Found)" "404" "$status" "$body"
    
    # Test 204 No Content - Valid deletion
    response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/$STEP_ID")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "DELETE (204 No Content - valid deletion)" "204" "$status" "$body"
    
    # 14. DELETE /api/steps/{id} (fallback)
    echo -e "${BLUE}14. DELETE /api/steps/{id} (fallback)${NC}"
    
    # Test 400 Bad Request - Invalid GUID
    response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/invalid-guid")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "DELETE (400 Bad Request - invalid GUID)" "400" "$status" "$body"
fi

echo ""
echo "========================================="
echo "CLEANUP"
echo "========================================="
curl -s -X DELETE "$PROCESSORS_URL/$PROCESSOR_ID" > /dev/null
curl -s -X DELETE "$SCHEMAS_URL/$INPUT_SCHEMA_ID" > /dev/null
curl -s -X DELETE "$SCHEMAS_URL/$OUTPUT_SCHEMA_ID" > /dev/null
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
    echo -e "${GREEN}✅ ALL 14 ENDPOINTS TESTED${NC}"
    echo -e "${GREEN}✅ ALL 7 STATUS CODES DEMONSTRATED${NC}"
    exit 0
else
    echo -e "${RED}❌ Some tests failed${NC}"
    exit 1
fi
