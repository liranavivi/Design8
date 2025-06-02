#!/bin/bash

# Comprehensive curl tests for DeliveriesController API
# Tests all endpoints and status codes

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
BASE_URL="http://localhost:5130/api/deliveries"
SCHEMAS_URL="http://localhost:5130/api/schemas"

# Test counters
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Function to print test results
print_result() {
    local expected=$1
    local actual=$2
    local test_name=$3
    local response_body=$4
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    if [ "$expected" = "$actual" ]; then
        echo -e "✅ ${GREEN}PASS${NC}: $test_name (Expected: $expected, Got: $actual)"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo -e "❌ ${RED}FAIL${NC}: $test_name (Expected: $expected, Got: $actual)"
        if [ -n "$response_body" ]; then
            echo -e "   ${YELLOW}Response:${NC} $response_body"
        fi
        FAILED_TESTS=$((FAILED_TESTS + 1))
    fi
}

# Function to print section headers
print_section() {
    echo ""
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}"
}

# Function to print test summary
print_summary() {
    echo ""
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}TEST SUMMARY${NC}"
    echo -e "${BLUE}========================================${NC}"
    echo -e "Total Tests: $TOTAL_TESTS"
    echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
    echo -e "${RED}Failed: $FAILED_TESTS${NC}"
    
    if [ $FAILED_TESTS -eq 0 ]; then
        echo -e "${GREEN}🎉 ALL TESTS PASSED! 🎉${NC}"
    else
        echo -e "${RED}❌ Some tests failed${NC}"
    fi
}

echo -e "${BLUE}🚀 Starting Deliveries Controller API Tests${NC}"
echo "Base URL: $BASE_URL"
echo ""

# Check if API is running
echo "Checking if API is running..."
response=$(curl -s -w "%{http_code}" -o /dev/null "$BASE_URL" || echo "000")
if [ "$response" != "200" ]; then
    echo -e "${RED}❌ API is not running or not accessible at $BASE_URL${NC}"
    echo "Please start the API first: dotnet run --project src/Presentation/FlowOrchestrator.EntitiesManagers.Api"
    exit 1
fi
echo -e "${GREEN}✅ API is running${NC}"

# ========================================
# SETUP TEST DATA
# ========================================
print_section "SETTING UP TEST DATA"

# Create a test schema first (required for foreign key)
echo "Creating test schema..."
SCHEMA_RESPONSE=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d '{
        "version": "1.0.0",
        "name": "TestSchemaForDeliveries",
        "definition": "{\"type\": \"object\", \"properties\": {\"test\": {\"type\": \"string\"}}}",
        "description": "Test schema for delivery testing"
    }' \
    "$SCHEMAS_URL" -o /tmp/schema_response.json)

if [ "$SCHEMA_RESPONSE" = "201" ]; then
    SCHEMA_ID=$(cat /tmp/schema_response.json | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "✅ Test schema created with ID: $SCHEMA_ID"
elif [ "$SCHEMA_RESPONSE" = "409" ]; then
    echo "⚠️ Test schema already exists, getting existing schema..."
    curl -s "$SCHEMAS_URL" -o /tmp/existing_schemas.json
    SCHEMA_ID=$(cat /tmp/existing_schemas.json | grep -A 10 -B 10 "TestSchemaForDeliveries" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    echo "✅ Using existing schema ID: $SCHEMA_ID"
else
    echo "❌ Failed to create test schema (status: $SCHEMA_RESPONSE)"
    echo "Response:"
    cat /tmp/schema_response.json
    exit 1
fi

if [ -z "$SCHEMA_ID" ]; then
    echo "❌ Could not get schema ID"
    exit 1
fi

echo "Using Schema ID: $SCHEMA_ID"

# ========================================
# TEST 1: GET /api/deliveries
# ========================================
print_section "TEST 1: GET /api/deliveries"

echo "Testing 200 OK - Get all deliveries"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL")
print_result "200" "$response" "Get all deliveries"

# ========================================
# TEST 2: GET /api/deliveries/paged
# ========================================
print_section "TEST 2: GET /api/deliveries/paged"

echo "Testing 200 OK - Valid pagination parameters"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/paged?page=1&pageSize=10")
print_result "200" "$response" "Valid pagination parameters"

echo "Testing 400 Bad Request - Invalid page parameter (0)"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/paged?page=0&pageSize=10")
print_result "400" "$response" "Invalid page parameter (0)"
echo "Response body:"
cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Invalid pageSize parameter (101)"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/paged?page=1&pageSize=101")
print_result "400" "$response" "Invalid pageSize parameter (101)"
echo "Response body:"
cat /tmp/response.json
echo ""

# ========================================
# TEST 3: POST /api/deliveries (Create test delivery)
# ========================================
print_section "TEST 3: POST /api/deliveries - CREATE TEST DELIVERY"

echo "Testing 201 Created - Valid delivery entity"
response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d "{
        \"version\": \"1.0.0\",
        \"name\": \"TestDeliveryForTesting\",
        \"schemaId\": \"$SCHEMA_ID\",
        \"payload\": \"{\\\"test\\\": \\\"data\\\"}\",
        \"description\": \"Test delivery for API testing\"
    }" \
    "$BASE_URL" -o /tmp/delivery_response.json)

if [ "$response" = "201" ]; then
    DELIVERY_ID=$(cat /tmp/delivery_response.json | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "✅ Test delivery created with ID: $DELIVERY_ID"
elif [ "$response" = "409" ]; then
    echo "⚠️ Test delivery already exists, getting existing delivery..."
    curl -s "$BASE_URL" -o /tmp/existing_deliveries.json
    DELIVERY_ID=$(cat /tmp/existing_deliveries.json | grep -A 10 -B 10 "TestDeliveryForTesting" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    echo "✅ Using existing delivery ID: $DELIVERY_ID"
else
    echo "❌ Failed to create test delivery (status: $response)"
    echo "Response:"
    cat /tmp/delivery_response.json
    exit 1
fi

print_result "201" "$response" "Valid delivery entity"

# ========================================
# TEST 4: GET /api/deliveries/{id}
# ========================================
print_section "TEST 4: GET /api/deliveries/{id}"

echo "Testing 200 OK - Get existing delivery by ID"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/$DELIVERY_ID")
print_result "200" "$response" "Get existing delivery by ID"

echo "Testing 404 Not Found - Non-existent delivery ID"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/00000000-0000-0000-0000-000000000000")
print_result "404" "$response" "Non-existent delivery ID"

# ========================================
# TEST 5: GET /api/deliveries/composite/{version}/{name}
# ========================================
print_section "TEST 5: GET /api/deliveries/composite/{version}/{name}"

echo "Testing 200 OK - Get delivery by composite key"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/composite/1.0.0/TestDeliveryForTesting")
print_result "200" "$response" "Get delivery by composite key"

echo "Testing 404 Not Found - Non-existent composite key"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/composite/999.0.0/NonExistentDelivery")
print_result "404" "$response" "Non-existent composite key"

echo "Testing 400 Bad Request - Empty name parameter"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/composite/1.0.0/")
print_result "400" "$response" "Empty name parameter"
echo "Response body:"
cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Empty composite key parameters"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/composite")
print_result "400" "$response" "Empty composite key parameters"
echo "Response body:"
cat /tmp/response.json
echo ""

# ========================================
# TEST 6: GET /api/deliveries/version/{version}
# ========================================
print_section "TEST 6: GET /api/deliveries/version/{version}"

echo "Testing 200 OK - Get deliveries by version"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/version/1.0.0")
print_result "200" "$response" "Get deliveries by version"

echo "Testing 200 OK - Valid version parameter (returns empty array if not found)"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/version/999.0.0")
print_result "200" "$response" "Valid version parameter (returns empty array if not found)"

echo "Testing 400 Bad Request - Empty version parameter"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/version")
print_result "400" "$response" "Empty version parameter"
echo "Response body:"
cat /tmp/response.json
echo ""

# ========================================
# TEST 7: GET /api/deliveries/name/{name}
# ========================================
print_section "TEST 7: GET /api/deliveries/name/{name}"

echo "Testing 200 OK - Get deliveries by name"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/name/TestDeliveryForTesting")
print_result "200" "$response" "Get deliveries by name"

echo "Testing 200 OK - Valid name parameter (returns empty array if not found)"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/name/NonExistentDeliveryName")
print_result "200" "$response" "Valid name parameter (returns empty array if not found)"

echo "Testing 400 Bad Request - Empty name parameter"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/name")
print_result "400" "$response" "Empty name parameter"
echo "Response body:"
cat /tmp/response.json
echo ""

# ========================================
# TEST 8: POST /api/deliveries - VALIDATION TESTS
# ========================================
print_section "TEST 8: POST /api/deliveries - VALIDATION TESTS"

echo "Testing 400 Bad Request - Null entity"
response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d 'null' \
    "$BASE_URL" -o /tmp/response.json)
print_result "400" "$response" "Null entity"
echo "Response body:"
cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Invalid model state (missing required fields)"
response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d '{}' \
    "$BASE_URL" -o /tmp/response.json)
print_result "400" "$response" "Invalid model state (missing required fields)"
echo "Response body:"
cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Foreign key validation failed (invalid SchemaId)"
response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d '{
        "version": "1.0.0",
        "name": "TestDeliveryInvalidSchema",
        "schemaId": "00000000-0000-0000-0000-000000000000",
        "payload": "{\"test\": \"data\"}",
        "description": "Test delivery with invalid schema"
    }' \
    "$BASE_URL" -o /tmp/response.json)
print_result "400" "$response" "Foreign key validation failed (invalid SchemaId)"
echo "Response body:"
cat /tmp/response.json
echo ""

echo "Testing 409 Conflict - Duplicate composite key"
response=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d "{
        \"version\": \"1.0.0\",
        \"name\": \"TestDeliveryForTesting\",
        \"schemaId\": \"$SCHEMA_ID\",
        \"payload\": \"{\\\"test\\\": \\\"data\\\"}\",
        \"description\": \"Duplicate test delivery\"
    }" \
    "$BASE_URL" -o /tmp/response.json)
print_result "409" "$response" "Duplicate composite key"
echo "Response body:"
cat /tmp/response.json
echo ""

# ========================================
# TEST 9: PUT /api/deliveries/{id} - UPDATE TESTS
# ========================================
print_section "TEST 9: PUT /api/deliveries/{id} - UPDATE TESTS"

echo "Testing 200 OK - Valid update"
response=$(curl -s -w "%{http_code}" -X PUT \
    -H "Content-Type: application/json" \
    -d "{
        \"id\": \"$DELIVERY_ID\",
        \"version\": \"1.0.1\",
        \"name\": \"TestDeliveryForTesting\",
        \"schemaId\": \"$SCHEMA_ID\",
        \"payload\": \"{\\\"test\\\": \\\"updated_data\\\"}\",
        \"description\": \"Updated test delivery for API testing\"
    }" \
    "$BASE_URL/$DELIVERY_ID" -o /tmp/response.json)
print_result "200" "$response" "Valid update"

echo "Testing 400 Bad Request - Null entity"
response=$(curl -s -w "%{http_code}" -X PUT \
    -H "Content-Type: application/json" \
    -d 'null' \
    "$BASE_URL/$DELIVERY_ID" -o /tmp/response.json)
print_result "400" "$response" "Null entity"

echo "Testing 400 Bad Request - ID mismatch"
response=$(curl -s -w "%{http_code}" -X PUT \
    -H "Content-Type: application/json" \
    -d "{
        \"id\": \"00000000-0000-0000-0000-000000000000\",
        \"version\": \"1.0.0\",
        \"name\": \"TestDeliveryForTesting\",
        \"schemaId\": \"$SCHEMA_ID\",
        \"payload\": \"{\\\"test\\\": \\\"data\\\"}\",
        \"description\": \"Test delivery\"
    }" \
    "$BASE_URL/$DELIVERY_ID" -o /tmp/response.json)
print_result "400" "$response" "ID mismatch"

echo "Testing 400 Bad Request - Foreign key validation failed"
response=$(curl -s -w "%{http_code}" -X PUT \
    -H "Content-Type: application/json" \
    -d "{
        \"id\": \"$DELIVERY_ID\",
        \"version\": \"1.0.0\",
        \"name\": \"TestDeliveryForTesting\",
        \"schemaId\": \"00000000-0000-0000-0000-000000000000\",
        \"payload\": \"{\\\"test\\\": \\\"data\\\"}\",
        \"description\": \"Test delivery\"
    }" \
    "$BASE_URL/$DELIVERY_ID" -o /tmp/response.json)
print_result "400" "$response" "Foreign key validation failed"

echo "Testing 404 Not Found - Non-existent ID"
response=$(curl -s -w "%{http_code}" -X PUT \
    -H "Content-Type: application/json" \
    -d "{
        \"id\": \"00000000-0000-0000-0000-000000000000\",
        \"version\": \"1.0.0\",
        \"name\": \"TestDeliveryForTesting\",
        \"schemaId\": \"$SCHEMA_ID\",
        \"payload\": \"{\\\"test\\\": \\\"data\\\"}\",
        \"description\": \"Test delivery\"
    }" \
    "$BASE_URL/00000000-0000-0000-0000-000000000000" -o /tmp/response.json)
print_result "404" "$response" "Non-existent ID"

# ========================================
# TEST 10: DELETE /api/deliveries/{id} - DELETE TESTS
# ========================================
print_section "TEST 10: DELETE /api/deliveries/{id} - DELETE TESTS"

echo "Testing 404 Not Found - Non-existent ID (before deletion)"
response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/00000000-0000-0000-0000-000000000000" -o /tmp/response.json)
print_result "404" "$response" "Non-existent ID (before deletion)"

echo "Testing 204 No Content - Valid deletion"
response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/$DELIVERY_ID" -o /tmp/response.json)
print_result "204" "$response" "Valid deletion"

echo "Testing 404 Not Found - Already deleted delivery"
response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/$DELIVERY_ID" -o /tmp/response.json)
print_result "404" "$response" "Already deleted delivery"

# ========================================
# CLEANUP
# ========================================
print_section "CLEANUP"

echo "Cleaning up test data..."

# Clean up test schema
echo "Deleting test schema..."
response=$(curl -s -w "%{http_code}" -X DELETE "$SCHEMAS_URL/$SCHEMA_ID" -o /tmp/response.json)
if [ "$response" = "204" ]; then
    echo "✅ Test schema deleted successfully"
else
    echo "⚠️ Could not delete test schema (status: $response)"
fi

echo "✅ Cleanup completed"

# ========================================
# SUMMARY
# ========================================
print_summary
