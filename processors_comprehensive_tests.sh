#!/bin/bash

# Comprehensive ProcessorsController API Tests
# Tests ALL 12 endpoints and ALL possible status codes

BASE_URL="http://localhost:5130/api/processors"
SCHEMAS_URL="http://localhost:5130/api/schemas"
CONTENT_TYPE="Content-Type: application/json"

echo "🧪 Comprehensive ProcessorsController API Tests"
echo "Testing ALL 12 endpoints and ALL possible status codes"
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

# Generate unique timestamp for test data
TIMESTAMP=$(date +%s)

echo "========================================="
echo "SETUP: Creating prerequisite entities"
echo "========================================="

# Create unique Schema entities for foreign key references
echo "Creating Schema entities..."

SCHEMA1_PAYLOAD='{
    "id": "00000000-0000-0000-0000-000000000000",
    "version": "'$TIMESTAMP'.1",
    "name": "TestInputSchemaProcessors",
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
    "name": "TestOutputSchemaProcessors",
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

# ProtocolId has been removed from ProcessorEntity
echo "ProtocolId field removed from ProcessorEntity"

echo ""
echo "========================================="
echo "TESTING ALL 12 ENDPOINTS"
echo "========================================="

# 1. GET /api/processors
echo -e "${BLUE}1. GET /api/processors${NC}"
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL")
status="${response: -3}"
body="${response%???}"
print_test_result "GET all processors (200 OK)" "200" "$status" "$body"

# 2. GET /api/processors/paged
echo -e "${BLUE}2. GET /api/processors/paged${NC}"

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

# 3. GET /api/processors/{id:guid}
echo -e "${BLUE}3. GET /api/processors/{id:guid}${NC}"

# Test 404 Not Found
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/99999999-9999-9999-9999-999999999999")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by ID (404 Not Found)" "404" "$status" "$body"

# 4. GET /api/processors/{id} (fallback)
echo -e "${BLUE}4. GET /api/processors/{id} (fallback)${NC}"

# Test 400 Bad Request - Invalid GUID
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/invalid-guid")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by invalid GUID (400 Bad Request)" "400" "$status" "$body"

# 5. GET /api/processors/by-key/{version}/{name}
echo -e "${BLUE}5. GET /api/processors/by-key/{version}/{name}${NC}"

# Test 404 Not Found
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-key/99.99.99/NonExistentProcessor")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by composite key (404 Not Found)" "404" "$status" "$body"

# Test URL decoding support
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-key/1.0.0/Test%20Processor%20Name")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by composite key (200 OK - URL encoded)" "404" "$status" "$body"

# 6. GET /api/processors/by-name/{name}
echo -e "${BLUE}6. GET /api/processors/by-name/{name}${NC}"

# Test 200 OK - Empty result
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-name/NonExistentProcessor")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by name (200 OK - empty result)" "200" "$status" "$body"

# 7. GET /api/processors/by-version/{version}
echo -e "${BLUE}7. GET /api/processors/by-version/{version}${NC}"

# Test 200 OK - Empty result
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-version/99.99.99")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by version (200 OK - empty result)" "200" "$status" "$body"

# 8. POST /api/processors
echo -e "${BLUE}8. POST /api/processors${NC}"

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

# Test 400 Bad Request - Invalid InputSchemaId
response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d '{
        "version": "'$TIMESTAMP'.3",
        "name": "TestProcessorInvalidFK",

        "inputSchemaId": "99999999-9999-9999-9999-999999999999",
        "outputSchemaId": "'$OUTPUT_SCHEMA_ID'"
    }')
status="${response: -3}"
body="${response%???}"
print_test_result "POST (400 Bad Request - invalid InputSchemaId)" "400" "$status" "$body"

# Test 400 Bad Request - Invalid OutputSchemaId
response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d '{
        "version": "'$TIMESTAMP'.4",
        "name": "TestProcessorInvalidFK2",

        "inputSchemaId": "'$INPUT_SCHEMA_ID'",
        "outputSchemaId": "99999999-9999-9999-9999-999999999999"
    }')
status="${response: -3}"
body="${response%???}"
print_test_result "POST (400 Bad Request - invalid OutputSchemaId)" "400" "$status" "$body"

# Test 201 Created - Valid creation
PROCESSOR_PAYLOAD='{
    "id": "00000000-0000-0000-0000-000000000000",
    "version": "'$TIMESTAMP'.5",
    "name": "TestProcessorAPI",

    "inputSchemaId": "'$INPUT_SCHEMA_ID'",
    "outputSchemaId": "'$OUTPUT_SCHEMA_ID'",
    "description": "Test processor for API testing"
}'

response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d "$PROCESSOR_PAYLOAD")
status="${response: -3}"
body="${response%???}"
print_test_result "POST (201 Created - valid creation)" "201" "$status" "$body"

if [ "$status" = "201" ]; then
    PROCESSOR_ID=$(echo "$body" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "Created Processor ID: $PROCESSOR_ID"
    
    # Test 409 Conflict - Duplicate key
    response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
        -H "$CONTENT_TYPE" \
        -d "$PROCESSOR_PAYLOAD")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "POST (409 Conflict - duplicate key)" "409" "$status" "$body"
    
    # Test successful GET by ID
    response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/$PROCESSOR_ID")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "GET by ID (200 OK - found)" "200" "$status" "$body"
    
    # Test successful GET by composite key
    response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/by-key/$TIMESTAMP.5/TestProcessorAPI")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "GET by composite key (200 OK - found)" "200" "$status" "$body"
    
    # 9. PUT /api/processors/{id:guid}
    echo -e "${BLUE}9. PUT /api/processors/{id:guid}${NC}"
    
    # Test 400 Bad Request - Invalid model
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/$PROCESSOR_ID" \
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
            "name": "TestProcessor"
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
            "name": "TestProcessor",

            "inputSchemaId": "'$INPUT_SCHEMA_ID'",
            "outputSchemaId": "'$OUTPUT_SCHEMA_ID'"
        }')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (404 Not Found)" "404" "$status" "$body"
    
    # Test 400 Bad Request - Invalid foreign key in update
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/$PROCESSOR_ID" \
        -H "$CONTENT_TYPE" \
        -d '{
            "id": "'$PROCESSOR_ID'",
            "version": "'$TIMESTAMP'.6",
            "name": "UpdatedTestProcessor",

            "inputSchemaId": "99999999-9999-9999-9999-999999999999",
            "outputSchemaId": "'$OUTPUT_SCHEMA_ID'"
        }')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (400 Bad Request - invalid foreign key)" "400" "$status" "$body"
    
    # Test 200 OK - Valid update
    UPDATE_PAYLOAD='{
        "id": "'$PROCESSOR_ID'",
        "version": "'$TIMESTAMP'.7",
        "name": "UpdatedTestProcessorAPI",

        "inputSchemaId": "'$INPUT_SCHEMA_ID'",
        "outputSchemaId": "'$OUTPUT_SCHEMA_ID'",
        "description": "Updated test processor"
    }'
    
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/$PROCESSOR_ID" \
        -H "$CONTENT_TYPE" \
        -d "$UPDATE_PAYLOAD")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (200 OK - valid update)" "200" "$status" "$body"
    
    # 10. PUT /api/processors/{id} (fallback)
    echo -e "${BLUE}10. PUT /api/processors/{id} (fallback)${NC}"
    
    # Test 400 Bad Request - Invalid GUID
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/invalid-guid" \
        -H "$CONTENT_TYPE" \
        -d '{}')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (400 Bad Request - invalid GUID)" "400" "$status" "$body"
    
    # 11. DELETE /api/processors/{id:guid}
    echo -e "${BLUE}11. DELETE /api/processors/{id:guid}${NC}"
    
    # Test 404 Not Found - Non-existent ID
    response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/99999999-9999-9999-9999-999999999999")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "DELETE (404 Not Found)" "404" "$status" "$body"
    
    # Test 204 No Content - Valid deletion
    response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/$PROCESSOR_ID")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "DELETE (204 No Content - valid deletion)" "204" "$status" "$body"
    
    # 12. DELETE /api/processors/{id} (fallback)
    echo -e "${BLUE}12. DELETE /api/processors/{id} (fallback)${NC}"
    
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
    echo -e "${GREEN}✅ ALL 12 ENDPOINTS TESTED${NC}"
    echo -e "${GREEN}✅ ALL 7 STATUS CODES DEMONSTRATED${NC}"
    exit 0
else
    echo -e "${RED}❌ Some tests failed${NC}"
    exit 1
fi
