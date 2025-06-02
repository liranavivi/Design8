#!/bin/bash

# AssignmentsController API Test Runner
# This script sets up the Docker environment and runs comprehensive API tests

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

echo -e "${BLUE}=== AssignmentsController API Test Runner ===${NC}"
echo -e "${BLUE}Project Root: $PROJECT_ROOT${NC}"
echo -e "${BLUE}Docker Directory: $SCRIPT_DIR${NC}"
echo ""

# Function to cleanup Docker resources
cleanup() {
    echo -e "${YELLOW}Cleaning up Docker infrastructure services...${NC}"
    cd "$SCRIPT_DIR"
    docker-compose -f docker-compose.yml -f docker-compose.test.yml down --volumes --remove-orphans 2>/dev/null || true
    echo -e "${YELLOW}Note: Local API (if running) is not affected by cleanup.${NC}"
}

# Function to check if Docker is running
check_docker() {
    if ! docker info >/dev/null 2>&1; then
        echo -e "${RED}Error: Docker is not running. Please start Docker and try again.${NC}"
        exit 1
    fi
    echo -e "${GREEN}Docker is running.${NC}"
}

# Function to start infrastructure services and check API
start_services() {
    echo -e "${BLUE}Starting infrastructure services...${NC}"
    cd "$SCRIPT_DIR"

    # Start only infrastructure services (API runs locally)
    echo -e "${BLUE}Starting infrastructure services (MongoDB, RabbitMQ, etc.)...${NC}"
    docker-compose -f docker-compose.yml -f docker-compose.test.yml up -d mongodb rabbitmq otel-collector hazelcast

    # Wait for infrastructure to be ready
    echo -e "${BLUE}Waiting for infrastructure services to be ready...${NC}"
    sleep 10

    # Check if local API is running
    echo -e "${BLUE}Checking if local API is running...${NC}"
    local max_attempts=5
    local attempt=1

    while [ $attempt -le $max_attempts ]; do
        if curl -s -f "http://localhost:5130/health" > /dev/null 2>&1; then
            echo -e "${GREEN}Local API is ready at http://localhost:5130!${NC}"
            return 0
        fi
        echo "Attempt $attempt/$max_attempts - API not ready yet..."
        sleep 2
        attempt=$((attempt + 1))
    done

    echo -e "${RED}Local API is not running at http://localhost:5130${NC}"
    echo -e "${YELLOW}Please start the API locally with:${NC}"
    echo -e "${CYAN}cd ..${NC}"
    echo -e "${CYAN}dotnet run --project src/Presentation/FlowOrchestrator.EntitiesManagers.Api/${NC}"
    echo ""
    echo -e "${YELLOW}The API should be available at:${NC}"
    echo -e "${CYAN}http://localhost:5130 (Development)${NC}"
    return 1
}

# Function to run tests
run_tests() {
    echo -e "${BLUE}Running comprehensive API test suite...${NC}"
    cd "$SCRIPT_DIR"

    # Make all test scripts executable
    chmod +x test-assignments-api.sh
    chmod +x test-assignments-edge-cases.sh
    chmod +x test-assignments-performance.sh
    chmod +x test-assignments-json-validation.sh
    chmod +x test-assignments-security.sh

    local all_tests_passed=true

    # Run core API tests
    echo -e "${BLUE}=== Running Core API Tests ===${NC}"
    if ./test-assignments-api.sh; then
        echo -e "${GREEN}✓ Core API tests passed${NC}"
    else
        echo -e "${RED}✗ Core API tests failed${NC}"
        all_tests_passed=false
    fi

    echo ""

    # Run edge case tests
    echo -e "${BLUE}=== Running Edge Case Tests ===${NC}"
    if ./test-assignments-edge-cases.sh; then
        echo -e "${GREEN}✓ Edge case tests passed${NC}"
    else
        echo -e "${RED}✗ Edge case tests failed${NC}"
        all_tests_passed=false
    fi

    echo ""

    # Run JSON validation tests
    echo -e "${BLUE}=== Running JSON Validation Tests ===${NC}"
    if ./test-assignments-json-validation.sh; then
        echo -e "${GREEN}✓ JSON validation tests passed${NC}"
    else
        echo -e "${RED}✗ JSON validation tests failed${NC}"
        all_tests_passed=false
    fi

    echo ""

    # Run performance tests
    echo -e "${BLUE}=== Running Performance Tests ===${NC}"
    if ./test-assignments-performance.sh; then
        echo -e "${GREEN}✓ Performance tests passed${NC}"
    else
        echo -e "${RED}✗ Performance tests failed${NC}"
        all_tests_passed=false
    fi

    echo ""

    # Run security tests
    echo -e "${BLUE}=== Running Security Tests ===${NC}"
    if ./test-assignments-security.sh; then
        echo -e "${GREEN}✓ Security tests passed${NC}"
    else
        echo -e "${RED}✗ Security tests failed${NC}"
        all_tests_passed=false
    fi

    echo ""

    if [ "$all_tests_passed" = true ]; then
        echo -e "${GREEN}🎉 All test suites completed successfully!${NC}"
        return 0
    else
        echo -e "${RED}❌ Some test suites failed.${NC}"
        return 1
    fi
}

# Function to show service logs
show_logs() {
    echo -e "${BLUE}Showing service logs...${NC}"
    cd "$SCRIPT_DIR"
    docker-compose -f docker-compose.yml -f docker-compose.test.yml logs
}

# Function to show service status
show_status() {
    echo -e "${BLUE}Service Status:${NC}"
    cd "$SCRIPT_DIR"
    docker-compose -f docker-compose.yml -f docker-compose.test.yml ps
}

# Main execution
main() {
    # Parse command line arguments
    case "${1:-}" in
        "cleanup")
            cleanup
            exit 0
            ;;
        "logs")
            show_logs
            exit 0
            ;;
        "status")
            show_status
            exit 0
            ;;
        "core")
            chmod +x test-assignments-api.sh
            ./test-assignments-api.sh
            exit $?
            ;;
        "edge")
            chmod +x test-assignments-edge-cases.sh
            ./test-assignments-edge-cases.sh
            exit $?
            ;;
        "performance")
            chmod +x test-assignments-performance.sh
            ./test-assignments-performance.sh
            exit $?
            ;;
        "json")
            chmod +x test-assignments-json-validation.sh
            ./test-assignments-json-validation.sh
            exit $?
            ;;
        "security")
            chmod +x test-assignments-security.sh
            ./test-assignments-security.sh
            exit $?
            ;;
        "help"|"-h"|"--help")
            echo "Usage: $0 [command]"
            echo ""
            echo "Commands:"
            echo "  (no args)   Run the complete test suite (all test types)"
            echo "  core        Run core API functionality tests only"
            echo "  edge        Run edge case tests only"
            echo "  performance Run performance tests only"
            echo "  json        Run JSON validation tests only"
            echo "  security    Run security tests only"
            echo "  cleanup     Clean up Docker resources"
            echo "  logs        Show service logs"
            echo "  status      Show service status"
            echo "  help        Show this help message"
            echo ""
            echo "Test Types:"
            echo "  • Core API Tests: All endpoints with status code validation"
            echo "  • Edge Cases: Validation, content type, pagination edge cases"
            echo "  • Performance: Response time and load testing"
            echo "  • JSON Validation: Response structure and data type validation"
            echo "  • Security: Basic security vulnerability testing"
            exit 0
            ;;
    esac
    
    # Trap to cleanup on exit
    trap cleanup EXIT
    
    # Check prerequisites
    check_docker
    
    # Start services
    if ! start_services; then
        echo -e "${RED}Failed to start services. Showing logs:${NC}"
        show_logs
        exit 1
    fi
    
    # Show service status
    show_status
    
    # Run tests
    if run_tests; then
        echo -e "${GREEN}=== Test Suite Completed Successfully ===${NC}"
        exit 0
    else
        echo -e "${RED}=== Test Suite Failed ===${NC}"
        echo -e "${YELLOW}Showing API logs for debugging:${NC}"
        docker-compose -f docker-compose.yml -f docker-compose.test.yml logs entitiesmanager-api
        exit 1
    fi
}

# Run main function
main "$@"
