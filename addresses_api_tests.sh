#!/bin/bash

# AddressesController API Tests
# Testing all endpoints and their possible status codes

BASE_URL="http://localhost:5130/api/addresses"

echo "=========================================="
echo "ADDRESSES CONTROLLER API TESTS"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print test header
print_test() {
    echo -e "${BLUE}=========================================="
    echo -e "TEST: $1"
    echo -e "==========================================${NC}"
}

# Function to print status code result
print_result() {
    local expected=$1
    local actual=$2
    local description=$3
    
    if [ "$expected" = "$actual" ]; then
        echo -e "${GREEN}✅ PASS${NC} - Expected: $expected, Got: $actual - $description"
    else
        echo -e "${RED}❌ FAIL${NC} - Expected: $expected, Got: $actual - $description"
    fi
    echo ""
}

# 1. GET /api/addresses - Get all addresses
print_test "1. GET /api/addresses - Get all addresses"

echo "Testing 200 OK - Successfully retrieve all addresses"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL")
print_result "200" "$response" "Get all addresses"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

# 2. GET /api/addresses/paged - Get paginated addresses
print_test "2. GET /api/addresses/paged - Get paginated addresses"

echo "Testing 200 OK - Valid pagination parameters"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/paged?page=1&pageSize=10")
print_result "200" "$response" "Valid pagination"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Invalid page (0)"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/paged?page=0&pageSize=10")
print_result "400" "$response" "Invalid page parameter (0)"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Invalid page (-1)"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/paged?page=-1&pageSize=10")
print_result "400" "$response" "Invalid page parameter (-1)"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Invalid pageSize (0)"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/paged?page=1&pageSize=0")
print_result "400" "$response" "Invalid pageSize parameter (0)"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Invalid pageSize (101)"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/paged?page=1&pageSize=101")
print_result "400" "$response" "Invalid pageSize parameter (101)"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

# 3. GET /api/addresses/{id} - Get address by ID
print_test "3. GET /api/addresses/{id} - Get address by ID"

echo "Testing 404 Not Found - Non-existent ID"
NON_EXISTENT_ID="12345678-1234-1234-1234-123456789012"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/$NON_EXISTENT_ID")
print_result "404" "$response" "Non-existent address ID"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

# 4. GET /api/addresses/by-address/{address} - Get by address
print_test "4. GET /api/addresses/by-address/{address} - Get by address"

echo "Testing 400 Bad Request - Empty address parameter"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-address/")
print_result "400" "$response" "Empty address parameter"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 200 OK - Valid address (returns empty array if not found)"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-address/test-address")
print_result "200" "$response" "Valid address parameter"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

# 5. GET /api/addresses/by-version/{version} - Get by version
print_test "5. GET /api/addresses/by-version/{version} - Get by version"

echo "Testing 400 Bad Request - Empty version parameter"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-version/")
print_result "400" "$response" "Empty version parameter"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 200 OK - Valid version (returns empty array if not found)"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-version/1.0.0")
print_result "200" "$response" "Valid version parameter"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

# 6. GET /api/addresses/by-name/{name} - Get by name
print_test "6. GET /api/addresses/by-name/{name} - Get by name"

echo "Testing 400 Bad Request - Empty name parameter"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-name/")
print_result "400" "$response" "Empty name parameter"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 200 OK - Valid name (returns empty array if not found)"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-name/test-name")
print_result "200" "$response" "Valid name parameter"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

# 7. GET /api/addresses/by-key/{address}/{version} - Get by composite key
print_test "7. GET /api/addresses/by-key/{address}/{version} - Get by composite key"

echo "Testing 400 Bad Request - Empty address in composite key"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-key//1.0.0")
print_result "400" "$response" "Empty address in composite key"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Empty version in composite key"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-key/test-address/")
print_result "400" "$response" "Empty version in composite key"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 404 Not Found - Non-existent composite key"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-key/non-existent-address/1.0.0")
print_result "404" "$response" "Non-existent composite key"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo ""
echo "=========================================="
echo "CONTINUING WITH POST/PUT/DELETE TESTS..."
echo "=========================================="

# First, let's create a valid schema to use for foreign key validation
echo "Creating a test schema for foreign key validation..."
SCHEMA_JSON='{
  "version": "1.0.0",
  "name": "TestSchema",
  "description": "Test schema for address validation",
  "schemaDefinition": "{\"type\": \"object\", \"properties\": {\"test\": {\"type\": \"string\"}}}"
}'

SCHEMA_RESPONSE=$(curl -s -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d "$SCHEMA_JSON" \
  -o /tmp/schema_response.json \
  "http://localhost:5130/api/schemas")

if [ "$SCHEMA_RESPONSE" = "201" ]; then
    SCHEMA_ID=$(cat /tmp/schema_response.json | jq -r '.id')
    echo "✅ Test schema created with ID: $SCHEMA_ID"
else
    echo "❌ Failed to create test schema. Using random GUID for tests."
    SCHEMA_ID="12345678-1234-1234-1234-123456789012"
fi
echo ""

# 8. POST /api/addresses - Create address
print_test "8. POST /api/addresses - Create address"

echo "Testing 400 Bad Request - Null entity"
response=$(curl -s -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d "null" \
  -o /tmp/response.json \
  "$BASE_URL")
print_result "400" "$response" "Null entity"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Invalid model state (missing required fields)"
INVALID_JSON='{
  "version": "",
  "name": "",
  "address": ""
}'
response=$(curl -s -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d "$INVALID_JSON" \
  -o /tmp/response.json \
  "$BASE_URL")
print_result "400" "$response" "Invalid model state"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Foreign key validation failed (invalid SchemaId)"
INVALID_SCHEMA_JSON='{
  "version": "1.0.0",
  "name": "TestAddress",
  "description": "Test address",
  "address": "test-address",
  "schemaId": "00000000-0000-0000-0000-000000000000"
}'
response=$(curl -s -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d "$INVALID_SCHEMA_JSON" \
  -o /tmp/response.json \
  "$BASE_URL")
print_result "400" "$response" "Foreign key validation failed"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 201 Created - Valid address entity"
VALID_JSON='{
  "version": "1.0.0",
  "name": "TestAddress",
  "description": "Test address for API testing",
  "address": "test-address-unique",
  "schemaId": "'$SCHEMA_ID'"
}'
response=$(curl -s -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d "$VALID_JSON" \
  -o /tmp/response.json \
  "$BASE_URL")
print_result "201" "$response" "Valid address entity"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json

# Extract the created address ID for later tests
if [ "$response" = "201" ]; then
    CREATED_ADDRESS_ID=$(cat /tmp/response.json | jq -r '.id')
    echo "✅ Created address ID: $CREATED_ADDRESS_ID"
else
    CREATED_ADDRESS_ID="00000000-0000-0000-0000-000000000000"
    echo "❌ Failed to create address. Using dummy ID for subsequent tests."
fi
echo ""

echo "Testing 409 Conflict - Duplicate composite key"
DUPLICATE_JSON='{
  "version": "1.0.0",
  "name": "TestAddress",
  "description": "Duplicate test address",
  "address": "test-address-unique",
  "schemaId": "'$SCHEMA_ID'"
}'
response=$(curl -s -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d "$DUPLICATE_JSON" \
  -o /tmp/response.json \
  "$BASE_URL")
print_result "409" "$response" "Duplicate composite key"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

# 9. PUT /api/addresses/{id} - Update address
print_test "9. PUT /api/addresses/{id} - Update address"

echo "Testing 400 Bad Request - Null entity"
response=$(curl -s -w "%{http_code}" -X PUT \
  -H "Content-Type: application/json" \
  -d "null" \
  -o /tmp/response.json \
  "$BASE_URL/$CREATED_ADDRESS_ID")
print_result "400" "$response" "Null entity"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - ID mismatch"
MISMATCH_JSON='{
  "id": "11111111-1111-1111-1111-111111111111",
  "version": "1.0.1",
  "name": "UpdatedAddress",
  "description": "Updated test address",
  "address": "updated-address",
  "schemaId": "'$SCHEMA_ID'"
}'
response=$(curl -s -w "%{http_code}" -X PUT \
  -H "Content-Type: application/json" \
  -d "$MISMATCH_JSON" \
  -o /tmp/response.json \
  "$BASE_URL/$CREATED_ADDRESS_ID")
print_result "400" "$response" "ID mismatch"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Invalid model state"
INVALID_UPDATE_JSON='{
  "id": "'$CREATED_ADDRESS_ID'",
  "version": "",
  "name": "",
  "address": "",
  "schemaId": "'$SCHEMA_ID'"
}'
response=$(curl -s -w "%{http_code}" -X PUT \
  -H "Content-Type: application/json" \
  -d "$INVALID_UPDATE_JSON" \
  -o /tmp/response.json \
  "$BASE_URL/$CREATED_ADDRESS_ID")
print_result "400" "$response" "Invalid model state"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 404 Not Found - Non-existent ID"
VALID_UPDATE_JSON='{
  "id": "'$NON_EXISTENT_ID'",
  "version": "1.0.1",
  "name": "UpdatedAddress",
  "description": "Updated test address",
  "address": "updated-address",
  "schemaId": "'$SCHEMA_ID'"
}'
response=$(curl -s -w "%{http_code}" -X PUT \
  -H "Content-Type: application/json" \
  -d "$VALID_UPDATE_JSON" \
  -o /tmp/response.json \
  "$BASE_URL/$NON_EXISTENT_ID")
print_result "404" "$response" "Non-existent ID"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Foreign key validation failed"
INVALID_FK_UPDATE_JSON='{
  "id": "'$CREATED_ADDRESS_ID'",
  "version": "1.0.1",
  "name": "UpdatedAddress",
  "description": "Updated test address",
  "address": "updated-address",
  "schemaId": "00000000-0000-0000-0000-000000000000"
}'
response=$(curl -s -w "%{http_code}" -X PUT \
  -H "Content-Type: application/json" \
  -d "$INVALID_FK_UPDATE_JSON" \
  -o /tmp/response.json \
  "$BASE_URL/$CREATED_ADDRESS_ID")
print_result "400" "$response" "Foreign key validation failed"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 200 OK - Valid update"
VALID_UPDATE_JSON='{
  "id": "'$CREATED_ADDRESS_ID'",
  "version": "1.0.1",
  "name": "UpdatedTestAddress",
  "description": "Updated test address for API testing",
  "address": "updated-test-address-unique",
  "schemaId": "'$SCHEMA_ID'"
}'
response=$(curl -s -w "%{http_code}" -X PUT \
  -H "Content-Type: application/json" \
  -d "$VALID_UPDATE_JSON" \
  -o /tmp/response.json \
  "$BASE_URL/$CREATED_ADDRESS_ID")
print_result "200" "$response" "Valid update"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

# Test GET by ID with the created address (should return 200 OK now)
echo "Testing 200 OK - Get created address by ID"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/$CREATED_ADDRESS_ID")
print_result "200" "$response" "Get created address by ID"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

# Test GET by composite key with the created address (should return 200 OK now)
echo "Testing 200 OK - Get created address by composite key"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-key/updated-test-address-unique/1.0.1")
print_result "200" "$response" "Get created address by composite key"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

# 10. DELETE /api/addresses/{id} - Delete address
print_test "10. DELETE /api/addresses/{id} - Delete address"

echo "Testing 404 Not Found - Non-existent ID"
response=$(curl -s -w "%{http_code}" -X DELETE \
  -o /tmp/response.json \
  "$BASE_URL/$NON_EXISTENT_ID")
print_result "404" "$response" "Non-existent ID"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 204 No Content - Valid deletion"
response=$(curl -s -w "%{http_code}" -X DELETE \
  -o /tmp/response.json \
  "$BASE_URL/$CREATED_ADDRESS_ID")
print_result "204" "$response" "Valid deletion"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

echo "Testing 404 Not Found - Already deleted address"
response=$(curl -s -w "%{http_code}" -X DELETE \
  -o /tmp/response.json \
  "$BASE_URL/$CREATED_ADDRESS_ID")
print_result "404" "$response" "Already deleted address"
echo "Response body:"
cat /tmp/response.json | jq . 2>/dev/null || cat /tmp/response.json
echo ""

# Cleanup - Delete the test schema
echo "Cleaning up test schema..."
if [ "$SCHEMA_ID" != "12345678-1234-1234-1234-123456789012" ]; then
    CLEANUP_RESPONSE=$(curl -s -w "%{http_code}" -X DELETE \
      -o /tmp/cleanup_response.json \
      "http://localhost:5130/api/schemas/$SCHEMA_ID")
    if [ "$CLEANUP_RESPONSE" = "204" ]; then
        echo "✅ Test schema deleted successfully"
    else
        echo "❌ Failed to delete test schema (status: $CLEANUP_RESPONSE)"
    fi
fi

echo ""
echo "=========================================="
echo "ALL TESTS COMPLETED"
echo "=========================================="
echo ""
echo "Summary of tested endpoints and status codes:"
echo "1. GET /api/addresses - 200 OK"
echo "2. GET /api/addresses/paged - 200 OK, 400 Bad Request"
echo "3. GET /api/addresses/{id} - 200 OK, 404 Not Found"
echo "4. GET /api/addresses/by-address/{address} - 200 OK, 400 Bad Request"
echo "5. GET /api/addresses/by-version/{version} - 200 OK, 400 Bad Request"
echo "6. GET /api/addresses/by-name/{name} - 200 OK, 400 Bad Request"
echo "7. GET /api/addresses/by-key/{address}/{version} - 200 OK, 400 Bad Request, 404 Not Found"
echo "8. POST /api/addresses - 201 Created, 400 Bad Request, 409 Conflict"
echo "9. PUT /api/addresses/{id} - 200 OK, 400 Bad Request, 404 Not Found"
echo "10. DELETE /api/addresses/{id} - 204 No Content, 404 Not Found"
echo ""
echo "Note: 500 Internal Server Error and 409 Conflict (referential integrity)"
echo "      scenarios require specific database states and are harder to reproduce"
echo "      in automated tests without complex setup."
