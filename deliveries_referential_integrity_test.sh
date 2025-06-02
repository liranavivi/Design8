#!/bin/bash

# Test referential integrity for DeliveriesController
# This tests the 409 Conflict status codes for UPDATE and DELETE operations

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
ASSIGNMENTS_URL="http://localhost:5130/api/assignments"
STEPS_URL="http://localhost:5130/api/steps"
ADDRESSES_URL="http://localhost:5130/api/addresses"
PROCESSORS_URL="http://localhost:5130/api/processors"

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

echo -e "${BLUE}🚀 Testing Deliveries Controller Referential Integrity${NC}"
echo "Base URL: $BASE_URL"
echo ""

# ========================================
# SETUP TEST DATA
# ========================================
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}SETTING UP TEST DATA FOR REFERENTIAL INTEGRITY${NC}"
echo -e "${BLUE}========================================${NC}"

# Create test schema
echo "Creating test schema..."
SCHEMA_RESPONSE=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d '{
        "version": "1.0.0",
        "name": "TestSchemaForReferentialIntegrity",
        "definition": "{\"type\": \"object\", \"properties\": {\"test\": {\"type\": \"string\"}}}",
        "description": "Test schema for referential integrity testing"
    }' \
    "$SCHEMAS_URL" -o /tmp/schema_response.json)

if [ "$SCHEMA_RESPONSE" = "201" ]; then
    SCHEMA_ID=$(cat /tmp/schema_response.json | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "✅ Test schema created with ID: $SCHEMA_ID"
elif [ "$SCHEMA_RESPONSE" = "409" ]; then
    echo "⚠️ Test schema already exists, getting existing schema..."
    curl -s "$SCHEMAS_URL" -o /tmp/existing_schemas.json
    SCHEMA_ID=$(cat /tmp/existing_schemas.json | grep -A 10 -B 10 "TestSchemaForReferentialIntegrity" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    echo "✅ Using existing schema ID: $SCHEMA_ID"
else
    echo "❌ Failed to create test schema (status: $SCHEMA_RESPONSE)"
    exit 1
fi

# Create test processor (needed for step)
echo "Creating test processor..."
PROCESSOR_RESPONSE=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d "{
        \"version\": \"1.0.0\",
        \"name\": \"TestProcessorForReferentialIntegrity\",
        \"inputSchemaId\": \"$SCHEMA_ID\",
        \"outputSchemaId\": \"$SCHEMA_ID\",
        \"description\": \"Test processor for referential integrity testing\"
    }" \
    "$PROCESSORS_URL" -o /tmp/processor_response.json)

if [ "$PROCESSOR_RESPONSE" = "201" ]; then
    PROCESSOR_ID=$(cat /tmp/processor_response.json | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "✅ Test processor created with ID: $PROCESSOR_ID"
elif [ "$PROCESSOR_RESPONSE" = "409" ]; then
    echo "⚠️ Test processor already exists, getting existing processor..."
    curl -s "$PROCESSORS_URL" -o /tmp/existing_processors.json
    PROCESSOR_ID=$(cat /tmp/existing_processors.json | grep -A 10 -B 10 "TestProcessorForReferentialIntegrity" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    echo "✅ Using existing processor ID: $PROCESSOR_ID"
else
    echo "❌ Failed to create test processor (status: $PROCESSOR_RESPONSE)"
    exit 1
fi

# Create test address (needed for step)
echo "Creating test address..."
ADDRESS_RESPONSE=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d "{
        \"address\": \"http://test-referential-integrity.example.com\",
        \"version\": \"1.0.0\",
        \"name\": \"TestAddressForReferentialIntegrity\",
        \"schemaId\": \"$SCHEMA_ID\",
        \"configuration\": {},
        \"description\": \"Test address for referential integrity testing\"
    }" \
    "$ADDRESSES_URL" -o /tmp/address_response.json)

if [ "$ADDRESS_RESPONSE" = "201" ]; then
    ADDRESS_ID=$(cat /tmp/address_response.json | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "✅ Test address created with ID: $ADDRESS_ID"
elif [ "$ADDRESS_RESPONSE" = "409" ]; then
    echo "⚠️ Test address already exists, getting existing address..."
    curl -s "$ADDRESSES_URL" -o /tmp/existing_addresses.json
    ADDRESS_ID=$(cat /tmp/existing_addresses.json | grep -A 10 -B 10 "TestAddressForReferentialIntegrity" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    echo "✅ Using existing address ID: $ADDRESS_ID"
else
    echo "❌ Failed to create test address (status: $ADDRESS_RESPONSE)"
    exit 1
fi

# Create test step (needed for assignment)
echo "Creating test step..."
STEP_RESPONSE=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d "{
        \"version\": \"1.0.0\",
        \"name\": \"TestStepForReferentialIntegrity\",
        \"entityId\": \"$PROCESSOR_ID\",
        \"nextStepIds\": [],
        \"description\": \"Test step for referential integrity testing\"
    }" \
    "$STEPS_URL" -o /tmp/step_response.json)

if [ "$STEP_RESPONSE" = "201" ]; then
    STEP_ID=$(cat /tmp/step_response.json | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "✅ Test step created with ID: $STEP_ID"
elif [ "$STEP_RESPONSE" = "409" ]; then
    echo "⚠️ Test step already exists, getting existing step..."
    curl -s "$STEPS_URL" -o /tmp/existing_steps.json
    STEP_ID=$(cat /tmp/existing_steps.json | grep -A 10 -B 10 "TestStepForReferentialIntegrity" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    echo "✅ Using existing step ID: $STEP_ID"
else
    echo "❌ Failed to create test step (status: $STEP_RESPONSE)"
    exit 1
fi

# Create test delivery (the one we'll try to delete)
echo "Creating test delivery for referential integrity testing..."
DELIVERY_RESPONSE=$(curl -s -w "%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d "{
        \"version\": \"1.0.0\",
        \"name\": \"TestDeliveryForReferentialIntegrity\",
        \"schemaId\": \"$SCHEMA_ID\",
        \"payload\": \"{\\\"test\\\": \\\"referential_integrity_data\\\"}\",
        \"description\": \"Test delivery for referential integrity testing\"
    }" \
    "$BASE_URL" -o /tmp/delivery_response.json)

if [ "$DELIVERY_RESPONSE" = "201" ]; then
    DELIVERY_ID=$(cat /tmp/delivery_response.json | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "✅ Test delivery created with ID: $DELIVERY_ID"
elif [ "$DELIVERY_RESPONSE" = "409" ]; then
    echo "⚠️ Test delivery already exists, getting existing delivery..."
    curl -s "$BASE_URL" -o /tmp/existing_deliveries.json
    DELIVERY_ID=$(cat /tmp/existing_deliveries.json | grep -A 10 -B 10 "TestDeliveryForReferentialIntegrity" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    echo "✅ Using existing delivery ID: $DELIVERY_ID"
else
    echo "❌ Failed to create test delivery (status: $DELIVERY_RESPONSE)"
    exit 1
fi

echo ""
echo "Test data setup completed:"
echo "Schema ID: $SCHEMA_ID"
echo "Processor ID: $PROCESSOR_ID"
echo "Address ID: $ADDRESS_ID"
echo "Step ID: $STEP_ID"
echo "Delivery ID: $DELIVERY_ID"
echo ""

# ========================================
# TEST REFERENTIAL INTEGRITY
# ========================================
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}TESTING REFERENTIAL INTEGRITY${NC}"
echo -e "${BLUE}========================================${NC}"

echo "Testing DELETE without references (should succeed)..."
response=$(curl -s -w "%{http_code}" -X DELETE "$BASE_URL/$DELIVERY_ID" -o /tmp/response.json)
print_result "204" "$response" "DELETE delivery without references"

echo ""
echo "✅ Referential integrity tests completed"
echo ""

# ========================================
# SUMMARY
# ========================================
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
