# AssignmentsController API Testing Suite

This directory contains comprehensive test scripts for testing all endpoints and status codes of the AssignmentsController API using Docker.

## Overview

The test suite covers all 11 endpoints of the AssignmentsController with tests for each possible HTTP status code:

### Endpoints Tested

1. **GET /api/assignments** - GetAll()
2. **GET /api/assignments/paged** - GetPaged()
3. **GET /api/assignments/{id:guid}** - GetById()
4. **GET /api/assignments/by-key/{stepId:guid}** - GetByCompositeKey()
5. **GET /api/assignments/by-step/{stepId:guid}** - GetByStepId()
6. **GET /api/assignments/by-entity/{entityId:guid}** - GetByEntityId()
7. **GET /api/assignments/by-name/{name}** - GetByName()
8. **GET /api/assignments/by-version/{version}** - GetByVersion()
9. **POST /api/assignments** - Create()
10. **PUT /api/assignments/{id:guid}** - Update()
11. **DELETE /api/assignments/{id:guid}** - Delete()

### Status Codes Tested

- **200 OK** - Successful operations
- **201 Created** - Successful creation
- **204 No Content** - Successful deletion
- **400 Bad Request** - Validation errors, malformed requests
- **404 Not Found** - Resource not found
- **409 Conflict** - Duplicate keys, referential integrity violations
- **500 Internal Server Error** - Server errors

## Files

### Core Test Scripts

- **`run-api-tests.sh`** - Main test runner that sets up Docker environment and runs all tests
- **`test-assignments-api.sh`** - Comprehensive test script covering all endpoints and status codes
- **`test-assignments-edge-cases.sh`** - Additional tests for edge cases and error scenarios

### Docker Configuration

- **`docker-compose.yml`** - Main Docker Compose file with API service added
- **`docker-compose.test.yml`** - Override configuration optimized for testing

## Prerequisites

1. **Docker and Docker Compose** installed and running (for infrastructure services)
2. **.NET SDK** installed (for running the API locally)
3. **curl** available in the system
4. **bash** shell environment
5. **jq** (for JSON validation tests)
6. **bc** (for performance calculations)

## Quick Start

### Step 1: Start the API Locally

```bash
# Navigate to the docker directory
cd docker

# Option A: Use the helper script (recommended)
./start-local-api.sh

# Option B: Manual startup
# Start infrastructure services
docker-compose up -d mongodb rabbitmq otel-collector hazelcast

# In another terminal, start the API
cd ..
dotnet run --project src/Presentation/FlowOrchestrator.EntitiesManagers.Api/
```

### Step 2: Run Tests (in another terminal)

```bash
# Navigate to the docker directory
cd docker

# Run all tests
./run-api-tests.sh
```

### Run Individual Test Scripts

```bash
# Run main API tests only
chmod +x test-assignments-api.sh
./test-assignments-api.sh

# Run edge case tests only
chmod +x test-assignments-edge-cases.sh
./test-assignments-edge-cases.sh
```

## Detailed Usage

### Main Test Runner (`run-api-tests.sh`)

The main test runner provides several commands:

```bash
# Run complete test suite (default)
./run-api-tests.sh

# Clean up Docker resources
./run-api-tests.sh cleanup

# Show service logs
./run-api-tests.sh logs

# Show service status
./run-api-tests.sh status

# Show help
./run-api-tests.sh help
```

### Test Configuration

The test environment uses:
- **API URL**: `http://localhost:5130` (Local development)
- **Database**: `EntitiesManagerTestDb` (separate from production)
- **Infrastructure**: Docker containers (MongoDB, RabbitMQ, etc.)
- **API**: Runs locally using .NET development server
- **Environment**: Development mode for detailed logging

## Test Categories

### 1. Basic Functionality Tests (`test-assignments-api.sh`)

- **GET Operations**: Test all read endpoints with valid and invalid parameters
- **POST Operations**: Test creation with valid data, validation errors, foreign key violations
- **PUT Operations**: Test updates with various scenarios including ID mismatches
- **DELETE Operations**: Test deletion with existing and non-existing resources

### 2. Edge Case Tests (`test-assignments-edge-cases.sh`)

- **Validation Edge Cases**: Null values, empty strings, field length limits
- **Content Type Issues**: Wrong content types, malformed JSON
- **HTTP Method Tests**: Unsupported methods, OPTIONS requests
- **Pagination Edge Cases**: Invalid page numbers, large page sizes
- **URL Encoding**: Special characters, Unicode in parameters
- **Large Payloads**: Testing with large data sets

## Expected Test Results

### Successful Test Run Output

```
=== AssignmentsController API Test Suite ===
Testing API at: http://localhost:5000

✓ PASS - GET /api/assignments - Success (Expected: 200, Got: 200)
✓ PASS - GET /api/assignments/paged - Success (Expected: 200, Got: 200)
✓ PASS - GET /api/assignments/paged - Invalid page (Expected: 400, Got: 400)
...

=== Test Summary ===
Total Tests: 22
Passed: 22
Failed: 0
All tests passed!
```

## Troubleshooting

### Common Issues

1. **API Not Ready**
   ```bash
   # Check service status
   ./run-api-tests.sh status
   
   # View logs
   ./run-api-tests.sh logs
   ```

2. **Port Conflicts**
   - Ensure port 5000 is not in use by other applications
   - Check Docker port mappings

3. **Docker Issues**
   ```bash
   # Clean up and restart
   ./run-api-tests.sh cleanup
   docker system prune -f
   ./run-api-tests.sh
   ```

### Manual Testing

You can also run individual curl commands manually:

```bash
# Test health endpoint
curl -f http://localhost:5130/health

# Test GET all assignments
curl -s http://localhost:5130/api/assignments

# Test POST with invalid data
curl -X POST \
  -H "Content-Type: application/json" \
  -d '{}' \
  http://localhost:5130/api/assignments
```

## Test Data

The test scripts automatically create test data including:
- Test StepEntity (required for foreign key relationships)
- Test AssignmentEntity for update/delete operations
- Various test scenarios with different data combinations

## Extending Tests

To add new test cases:

1. **Add to existing scripts**: Modify `test-assignments-api.sh` or `test-assignments-edge-cases.sh`
2. **Create new test script**: Follow the pattern of existing scripts
3. **Update main runner**: Modify `run-api-tests.sh` to include new test scripts

### Test Function Template

```bash
# Test description
response=$(curl -s -w "%{http_code}" [curl_options] "$API_ENDPOINT/path" 2>/dev/null || echo "000")
status_code="${response: -3}"
response_body="${response%???}"
print_test_result "Test Name" "expected_status" "$status_code" "$response_body"
```

## Integration with CI/CD

The test scripts are designed to be CI/CD friendly:
- Exit codes: 0 for success, 1 for failure
- Colored output can be disabled by setting `NO_COLOR=1`
- JSON output available for parsing results
- Docker cleanup handled automatically

Example CI usage:
```yaml
- name: Run API Tests
  run: |
    cd docker
    chmod +x run-api-tests.sh
    ./run-api-tests.sh
```
