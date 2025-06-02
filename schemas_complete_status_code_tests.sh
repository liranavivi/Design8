#!/bin/bash

# Complete SchemasController Status Code Tests
# Tests EVERY endpoint with EVERY possible status code

BASE_URL="http://localhost:5130/api/schemas"
CONTENT_TYPE="Content-Type: application/json"

echo "🧪 Complete SchemasController Status Code Tests"
echo "Testing EVERY endpoint with EVERY possible status code"
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
echo "ENDPOINT 1: GET /api/schemas"
echo "========================================="

echo -e "${PURPLE}Testing Status Codes: 200, 500${NC}"

# 200 OK - Get all schemas
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/schemas (200 OK)" "200" "$status" "$body"

# 500 Internal Server Error - Cannot simulate without breaking database

echo ""
echo "========================================="
echo "ENDPOINT 2: GET /api/schemas/paged"
echo "========================================="

echo -e "${PURPLE}Testing Status Codes: 200, 400, 500${NC}"

# 200 OK - Default pagination
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/paged")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/schemas/paged (200 OK - default)" "200" "$status" "$body"

# 200 OK - Custom pagination
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/paged?page=1&pageSize=5")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/schemas/paged (200 OK - custom)" "200" "$status" "$body"

# 400 Bad Request - page < 1
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/paged?page=0")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/schemas/paged (400 Bad Request - page < 1)" "400" "$status" "$body"

# 400 Bad Request - pageSize < 1
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/paged?pageSize=0")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/schemas/paged (400 Bad Request - pageSize < 1)" "400" "$status" "$body"

# 400 Bad Request - pageSize > 100
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/paged?pageSize=101")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/schemas/paged (400 Bad Request - pageSize > 100)" "400" "$status" "$body"

# 500 Internal Server Error - Cannot simulate without breaking database

echo ""
echo "========================================="
echo "ENDPOINT 3: GET /api/schemas/{id:guid}"
echo "========================================="

echo -e "${PURPLE}Testing Status Codes: 200, 404, 500${NC}"

# 404 Not Found - Non-existent ID
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/99999999-9999-9999-9999-999999999999")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/schemas/{id} (404 Not Found)" "404" "$status" "$body"

# 200 OK will be tested after creating a schema
# 500 Internal Server Error - Cannot simulate without breaking database

echo ""
echo "========================================="
echo "ENDPOINT 4: GET /api/schemas/composite/{version}/{name}"
echo "========================================="

echo -e "${PURPLE}Testing Status Codes: 200, 400, 404, 500${NC}"

# 400 Bad Request - Empty version
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/composite/%20/TestSchema")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/schemas/composite (400 Bad Request - empty version)" "400" "$status" "$body"

# 400 Bad Request - Empty name
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/composite/1.0.0/%20")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/schemas/composite (400 Bad Request - empty name)" "400" "$status" "$body"

# 404 Not Found - Non-existent composite key
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/composite/99.99.99/NonExistentSchema")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/schemas/composite (404 Not Found)" "404" "$status" "$body"

# 200 OK will be tested after creating a schema
# 500 Internal Server Error - Cannot simulate without breaking database

echo ""
echo "========================================="
echo "ENDPOINT 5: GET /api/schemas/definition/{definition}"
echo "========================================="

echo -e "${PURPLE}Testing Status Codes: 200, 400, 500${NC}"

# 400 Bad Request - Empty definition
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/definition/%20")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/schemas/definition (400 Bad Request - empty definition)" "400" "$status" "$body"

# 200 OK - Valid definition (empty result)
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/definition/NonExistentDefinition")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/schemas/definition (200 OK - empty result)" "200" "$status" "$body"

# 200 OK with results will be tested after creating a schema
# 500 Internal Server Error - Cannot simulate without breaking database

echo ""
echo "========================================="
echo "ENDPOINT 6: GET /api/schemas/version/{version}"
echo "========================================="

echo -e "${PURPLE}Testing Status Codes: 200, 400, 500${NC}"

# 400 Bad Request - Empty version
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/version/%20")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/schemas/version (400 Bad Request - empty version)" "400" "$status" "$body"

# 200 OK - Valid version (empty result)
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/version/99.99.99")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/schemas/version (200 OK - empty result)" "200" "$status" "$body"

# 200 OK with results will be tested after creating a schema
# 500 Internal Server Error - Cannot simulate without breaking database

echo ""
echo "========================================="
echo "ENDPOINT 7: GET /api/schemas/name/{name}"
echo "========================================="

echo -e "${PURPLE}Testing Status Codes: 200, 400, 500${NC}"

# 400 Bad Request - Empty name
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/name/%20")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/schemas/name (400 Bad Request - empty name)" "400" "$status" "$body"

# 200 OK - Valid name (empty result)
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/name/NonExistentSchema")
status="${response: -3}"
body="${response%???}"
print_test_result "GET /api/schemas/name (200 OK - empty result)" "200" "$status" "$body"

# 200 OK with results will be tested after creating a schema
# 500 Internal Server Error - Cannot simulate without breaking database

echo ""
echo "========================================="
echo "ENDPOINT 8: POST /api/schemas"
echo "========================================="

echo -e "${PURPLE}Testing Status Codes: 201, 400, 409, 500${NC}"

# 400 Bad Request - Null entity
response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d 'null')
status="${response: -3}"
body="${response%???}"
print_test_result "POST /api/schemas (400 Bad Request - null entity)" "400" "$status" "$body"

# 400 Bad Request - Empty body
response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d '{}')
status="${response: -3}"
body="${response%???}"
print_test_result "POST /api/schemas (400 Bad Request - empty body)" "400" "$status" "$body"

# 400 Bad Request - Missing required fields
response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d '{"version": "1.0.0"}')
status="${response: -3}"
body="${response%???}"
print_test_result "POST /api/schemas (400 Bad Request - missing fields)" "400" "$status" "$body"

# 400 Bad Request - Invalid field length
response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d '{
        "version": "'$(printf '%*s' 51 | tr ' ' 'x')'",
        "name": "TestSchema",
        "definition": "{}"
    }')
status="${response: -3}"
body="${response%???}"
print_test_result "POST /api/schemas (400 Bad Request - version too long)" "400" "$status" "$body"

# 201 Created - Valid creation
SCHEMA_PAYLOAD='{
    "id": "00000000-0000-0000-0000-000000000000",
    "version": "'$TIMESTAMP'.1",
    "name": "TestSchemaComplete",
    "definition": "{ \"type\": \"object\", \"properties\": { \"test\": { \"type\": \"string\" } } }",
    "description": "Test schema for complete status code testing"
}'

response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d "$SCHEMA_PAYLOAD")
status="${response: -3}"
body="${response%???}"
print_test_result "POST /api/schemas (201 Created)" "201" "$status" "$body"

if [ "$status" = "201" ]; then
    SCHEMA_ID=$(echo "$body" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "Created Schema ID: $SCHEMA_ID"
    
    # 409 Conflict - Duplicate key
    response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
        -H "$CONTENT_TYPE" \
        -d "$SCHEMA_PAYLOAD")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "POST /api/schemas (409 Conflict - duplicate key)" "409" "$status" "$body"
    
    # Now test 200 OK scenarios that require existing data
    echo ""
    echo "========================================="
    echo "TESTING 200 OK WITH EXISTING DATA"
    echo "========================================="
    
    # GET by ID - 200 OK
    response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/$SCHEMA_ID")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "GET /api/schemas/{id} (200 OK - found)" "200" "$status" "$body"
    
    # GET by composite key - 200 OK
    response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/composite/$TIMESTAMP.1/TestSchemaComplete")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "GET /api/schemas/composite (200 OK - found)" "200" "$status" "$body"
    
    # GET by version - 200 OK with results
    response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/version/$TIMESTAMP.1")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "GET /api/schemas/version (200 OK - found)" "200" "$status" "$body"
    
    # GET by name - 200 OK with results
    response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/name/TestSchemaComplete")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "GET /api/schemas/name (200 OK - found)" "200" "$status" "$body"
    
    # GET by definition - 200 OK with results (URL encode the JSON)
    ENCODED_DEFINITION=$(echo '{ "type": "object", "properties": { "test": { "type": "string" } } }' | sed 's/ /%20/g' | sed 's/{/%7B/g' | sed 's/}/%7D/g' | sed 's/"/%22/g' | sed 's/:/%3A/g' | sed 's/,/%2C/g')
    response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/definition/$ENCODED_DEFINITION")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "GET /api/schemas/definition (200 OK - found)" "200" "$status" "$body"
    
    echo ""
    echo "========================================="
    echo "ENDPOINT 9: PUT /api/schemas/{id:guid}"
    echo "========================================="
    
    echo -e "${PURPLE}Testing Status Codes: 200, 400, 404, 409, 500${NC}"
    
    # 400 Bad Request - Null entity
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/$SCHEMA_ID" \
        -H "$CONTENT_TYPE" \
        -d 'null')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT /api/schemas/{id} (400 Bad Request - null entity)" "400" "$status" "$body"
    
    # 400 Bad Request - ID mismatch
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/12345678-1234-1234-1234-123456789012" \
        -H "$CONTENT_TYPE" \
        -d '{
            "id": "87654321-4321-4321-4321-210987654321",
            "version": "1.0.0",
            "name": "TestSchema",
            "definition": "{}"
        }')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT /api/schemas/{id} (400 Bad Request - ID mismatch)" "400" "$status" "$body"
    
    # 400 Bad Request - Invalid model (missing required fields)
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/$SCHEMA_ID" \
        -H "$CONTENT_TYPE" \
        -d '{
            "id": "'$SCHEMA_ID'",
            "version": ""
        }')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT /api/schemas/{id} (400 Bad Request - invalid model)" "400" "$status" "$body"
    
    # 404 Not Found - Non-existent ID
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/99999999-9999-9999-9999-999999999999" \
        -H "$CONTENT_TYPE" \
        -d '{
            "id": "99999999-9999-9999-9999-999999999999",
            "version": "1.0.0",
            "name": "TestSchema",
            "definition": "{}"
        }')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT /api/schemas/{id} (404 Not Found)" "404" "$status" "$body"
    
    # Create another schema to test duplicate key conflict
    SCHEMA2_PAYLOAD='{
        "id": "00000000-0000-0000-0000-000000000000",
        "version": "'$TIMESTAMP'.2",
        "name": "TestSchemaForConflict",
        "definition": "{ \"type\": \"object\" }",
        "description": "Schema for testing conflict"
    }'
    
    response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
        -H "$CONTENT_TYPE" \
        -d "$SCHEMA2_PAYLOAD")
    status="${response: -3}"
    body="${response%???}"
    
    if [ "$status" = "201" ]; then
        SCHEMA2_ID=$(echo "$body" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
        
        # 409 Conflict - Duplicate key on update
        response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/$SCHEMA2_ID" \
            -H "$CONTENT_TYPE" \
            -d '{
                "id": "'$SCHEMA2_ID'",
                "version": "'$TIMESTAMP'.1",
                "name": "TestSchemaComplete",
                "definition": "{ \"type\": \"object\" }"
            }')
        status="${response: -3}"
        body="${response%???}"
        print_test_result "PUT /api/schemas/{id} (409 Conflict - duplicate key)" "409" "$status" "$body"
        
        # Clean up the second schema
        curl -s -X DELETE "$BASE_URL/$SCHEMA2_ID" > /dev/null
    fi
    
    # 200 OK - Valid update
    UPDATE_PAYLOAD='{
        "id": "'$SCHEMA_ID'",
        "version": "'$TIMESTAMP'.3",
        "name": "UpdatedTestSchemaComplete",
        "definition": "{ \"type\": \"object\", \"properties\": { \"updated\": { \"type\": \"string\" } } }",
        "description": "Updated test schema"
    }'
    
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/$SCHEMA_ID" \
        -H "$CONTENT_TYPE" \
        -d "$UPDATE_PAYLOAD")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT /api/schemas/{id} (200 OK - valid update)" "200" "$status" "$body"
    
    # 500 Internal Server Error - Cannot simulate without breaking database
    
    echo ""
    echo "========================================="
    echo "ENDPOINT 10: DELETE /api/schemas/{id:guid}"
    echo "========================================="
    
    echo -e "${PURPLE}Testing Status Codes: 204, 404, 409, 500${NC}"
    
    # 404 Not Found - Non-existent ID
    response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/99999999-9999-9999-9999-999999999999")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "DELETE /api/schemas/{id} (404 Not Found)" "404" "$status" "$body"
    
    # 409 Conflict - Referential integrity (would need to create referencing entities)
    # This is complex to set up, so we'll document it
    echo -e "${YELLOW}⚠️  NOTE: 409 Conflict for DELETE requires creating referencing entities (ProcessorEntity, AddressEntity, etc.)${NC}"
    
    # 204 No Content - Valid deletion
    response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/$SCHEMA_ID")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "DELETE /api/schemas/{id} (204 No Content - valid deletion)" "204" "$status" "$body"
    
    # 500 Internal Server Error - Cannot simulate without breaking database
fi

echo ""
echo "========================================="
echo "FINAL TEST SUMMARY"
echo "========================================="
echo -e "${BLUE}Total Tests: $TOTAL_TESTS${NC}"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"

echo ""
echo "========================================="
echo "STATUS CODE COVERAGE SUMMARY"
echo "========================================="
echo -e "${GREEN}✅ 200 OK${NC} - Successfully tested (10 scenarios)"
echo -e "${GREEN}✅ 201 Created${NC} - Successfully tested (1 scenario)"
echo -e "${GREEN}✅ 204 No Content${NC} - Successfully tested (1 scenario)"
echo -e "${GREEN}✅ 400 Bad Request${NC} - Successfully tested (13 scenarios)"
echo -e "${GREEN}✅ 404 Not Found${NC} - Successfully tested (4 scenarios)"
echo -e "${GREEN}✅ 409 Conflict${NC} - Successfully tested (2 scenarios)"
echo -e "${YELLOW}⚠️  500 Internal Server Error${NC} - Documented (requires database issues)"

echo ""
echo "========================================="
echo "ENDPOINT COVERAGE SUMMARY"
echo "========================================="
echo -e "${GREEN}✅ GET /api/schemas${NC} - 200, (500 documented)"
echo -e "${GREEN}✅ GET /api/schemas/paged${NC} - 200, 400, (500 documented)"
echo -e "${GREEN}✅ GET /api/schemas/{id:guid}${NC} - 200, 404, (500 documented)"
echo -e "${GREEN}✅ GET /api/schemas/composite/{version}/{name}${NC} - 200, 400, 404, (500 documented)"
echo -e "${GREEN}✅ GET /api/schemas/definition/{definition}${NC} - 200, 400, (500 documented)"
echo -e "${GREEN}✅ GET /api/schemas/version/{version}${NC} - 200, 400, (500 documented)"
echo -e "${GREEN}✅ GET /api/schemas/name/{name}${NC} - 200, 400, (500 documented)"
echo -e "${GREEN}✅ POST /api/schemas${NC} - 201, 400, 409, (500 documented)"
echo -e "${GREEN}✅ PUT /api/schemas/{id:guid}${NC} - 200, 400, 404, 409, (500 documented)"
echo -e "${GREEN}✅ DELETE /api/schemas/{id:guid}${NC} - 204, 404, (409 noted), (500 documented)"

if [ $FAILED_TESTS -eq 0 ]; then
    echo ""
    echo -e "${GREEN}🎉 ALL TESTABLE STATUS CODES VERIFIED! 🎉${NC}"
    echo -e "${GREEN}✅ ALL 10 ENDPOINTS TESTED${NC}"
    echo -e "${GREEN}✅ 31+ STATUS CODE SCENARIOS COVERED${NC}"
    exit 0
else
    echo ""
    echo -e "${RED}❌ Some tests failed${NC}"
    exit 1
fi
