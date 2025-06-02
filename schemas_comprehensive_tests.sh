#!/bin/bash

# Comprehensive SchemasController API Tests
# Tests ALL 10 endpoints and ALL possible status codes

BASE_URL="http://localhost:5130/api/schemas"
CONTENT_TYPE="Content-Type: application/json"

echo "🧪 Comprehensive SchemasController API Tests"
echo "Testing ALL 10 endpoints and ALL possible status codes"
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
echo "TESTING ALL 10 ENDPOINTS"
echo "========================================="

# 1. GET /api/schemas
echo -e "${BLUE}1. GET /api/schemas${NC}"
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL")
status="${response: -3}"
body="${response%???}"
print_test_result "GET all schemas (200 OK)" "200" "$status" "$body"

# 2. GET /api/schemas/paged
echo -e "${BLUE}2. GET /api/schemas/paged${NC}"

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

# 3. GET /api/schemas/{id:guid}
echo -e "${BLUE}3. GET /api/schemas/{id:guid}${NC}"

# Test 404 Not Found
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/99999999-9999-9999-9999-999999999999")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by ID (404 Not Found)" "404" "$status" "$body"

# 4. GET /api/schemas/composite/{version}/{name}
echo -e "${BLUE}4. GET /api/schemas/composite/{version}/{name}${NC}"

# Test 400 Bad Request - Empty version (using %20 for space)
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/composite/%20/TestSchema")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by composite key (400 Bad Request - empty version)" "400" "$status" "$body"

# Test 400 Bad Request - Empty name (using %20 for space)
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/composite/1.0.0/%20")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by composite key (400 Bad Request - empty name)" "400" "$status" "$body"

# Test 404 Not Found
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/composite/99.99.99/NonExistentSchema")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by composite key (404 Not Found)" "404" "$status" "$body"

# 5. GET /api/schemas/definition/{definition}
echo -e "${BLUE}5. GET /api/schemas/definition/{definition}${NC}"

# Test 400 Bad Request - Empty definition (using %20 for space)
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/definition/%20")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by definition (400 Bad Request - empty definition)" "400" "$status" "$body"

# Test 200 OK - Valid definition (empty result)
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/definition/NonExistentDefinition")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by definition (200 OK - empty result)" "200" "$status" "$body"

# 6. GET /api/schemas/version/{version}
echo -e "${BLUE}6. GET /api/schemas/version/{version}${NC}"

# Test 400 Bad Request - Empty version (using %20 for space)
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/version/%20")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by version (400 Bad Request - empty version)" "400" "$status" "$body"

# Test 200 OK - Valid version (empty result)
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/version/99.99.99")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by version (200 OK - empty result)" "200" "$status" "$body"

# 7. GET /api/schemas/name/{name}
echo -e "${BLUE}7. GET /api/schemas/name/{name}${NC}"

# Test 400 Bad Request - Empty name (using %20 for space)
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/name/%20")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by name (400 Bad Request - empty name)" "400" "$status" "$body"

# Test 200 OK - Valid name (empty result)
response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/name/NonExistentSchema")
status="${response: -3}"
body="${response%???}"
print_test_result "GET by name (200 OK - empty result)" "200" "$status" "$body"

# 8. POST /api/schemas
echo -e "${BLUE}8. POST /api/schemas${NC}"

# Test 400 Bad Request - Null entity
response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d 'null')
status="${response: -3}"
body="${response%???}"
print_test_result "POST (400 Bad Request - null entity)" "400" "$status" "$body"

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

# Test 201 Created - Valid creation
SCHEMA_PAYLOAD='{
    "id": "00000000-0000-0000-0000-000000000000",
    "version": "'$TIMESTAMP'.1",
    "name": "TestSchemaAPI",
    "definition": "{ \"type\": \"object\", \"properties\": { \"test\": { \"type\": \"string\" } } }",
    "description": "Test schema for API testing"
}'

response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
    -H "$CONTENT_TYPE" \
    -d "$SCHEMA_PAYLOAD")
status="${response: -3}"
body="${response%???}"
print_test_result "POST (201 Created - valid creation)" "201" "$status" "$body"

if [ "$status" = "201" ]; then
    SCHEMA_ID=$(echo "$body" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "Created Schema ID: $SCHEMA_ID"
    
    # Test 409 Conflict - Duplicate key
    response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL" \
        -H "$CONTENT_TYPE" \
        -d "$SCHEMA_PAYLOAD")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "POST (409 Conflict - duplicate key)" "409" "$status" "$body"
    
    # Test successful GET by ID
    response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/$SCHEMA_ID")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "GET by ID (200 OK - found)" "200" "$status" "$body"
    
    # Test successful GET by composite key
    response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/composite/$TIMESTAMP.1/TestSchemaAPI")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "GET by composite key (200 OK - found)" "200" "$status" "$body"
    
    # Test successful GET by version
    response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/version/$TIMESTAMP.1")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "GET by version (200 OK - found)" "200" "$status" "$body"
    
    # Test successful GET by name
    response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/name/TestSchemaAPI")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "GET by name (200 OK - found)" "200" "$status" "$body"
    
    # 9. PUT /api/schemas/{id:guid}
    echo -e "${BLUE}9. PUT /api/schemas/{id:guid}${NC}"
    
    # Test 400 Bad Request - Null entity
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/$SCHEMA_ID" \
        -H "$CONTENT_TYPE" \
        -d 'null')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (400 Bad Request - null entity)" "400" "$status" "$body"
    
    # Test 400 Bad Request - ID mismatch
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/12345678-1234-1234-1234-123456789012" \
        -H "$CONTENT_TYPE" \
        -d '{
            "id": "87654321-4321-4321-4321-210987654321",
            "version": "1.0.0",
            "name": "TestSchema"
        }')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (400 Bad Request - ID mismatch)" "400" "$status" "$body"
    
    # Test 400 Bad Request - Invalid model
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/$SCHEMA_ID" \
        -H "$CONTENT_TYPE" \
        -d '{
            "id": "'$SCHEMA_ID'",
            "version": ""
        }')
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (400 Bad Request - invalid model)" "400" "$status" "$body"
    
    # Test 404 Not Found - Non-existent ID
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
    print_test_result "PUT (404 Not Found)" "404" "$status" "$body"
    
    # Test 200 OK - Valid update
    UPDATE_PAYLOAD='{
        "id": "'$SCHEMA_ID'",
        "version": "'$TIMESTAMP'.2",
        "name": "UpdatedTestSchemaAPI",
        "definition": "{ \"type\": \"object\", \"properties\": { \"updated\": { \"type\": \"string\" } } }",
        "description": "Updated test schema"
    }'
    
    response=$(curl -s -w "%{http_code}" -X PUT "$BASE_URL/$SCHEMA_ID" \
        -H "$CONTENT_TYPE" \
        -d "$UPDATE_PAYLOAD")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "PUT (200 OK - valid update)" "200" "$status" "$body"
    
    # 10. DELETE /api/schemas/{id:guid}
    echo -e "${BLUE}10. DELETE /api/schemas/{id:guid}${NC}"
    
    # Test 404 Not Found - Non-existent ID
    response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/99999999-9999-9999-9999-999999999999")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "DELETE (404 Not Found)" "404" "$status" "$body"
    
    # Test 204 No Content - Valid deletion
    response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/$SCHEMA_ID")
    status="${response: -3}"
    body="${response%???}"
    print_test_result "DELETE (204 No Content - valid deletion)" "204" "$status" "$body"
fi

echo ""
echo "========================================="
echo "FINAL TEST SUMMARY"
echo "========================================="
echo -e "${BLUE}Total Tests: $TOTAL_TESTS${NC}"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}🎉 ALL TESTS PASSED! 🎉${NC}"
    echo -e "${GREEN}✅ ALL 10 ENDPOINTS TESTED${NC}"
    echo -e "${GREEN}✅ ALL 6 STATUS CODES DEMONSTRATED${NC}"
    exit 0
else
    echo -e "${RED}❌ Some tests failed${NC}"
    exit 1
fi
