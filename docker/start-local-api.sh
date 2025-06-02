#!/bin/bash

# Script to start the EntitiesManager API locally for testing

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
API_PROJECT_PATH="$PROJECT_ROOT/src/Presentation/FlowOrchestrator.EntitiesManagers.Api"

echo -e "${BLUE}=== EntitiesManager API Local Startup Script ===${NC}"
echo -e "${BLUE}Project Root: $PROJECT_ROOT${NC}"
echo -e "${BLUE}API Project: $API_PROJECT_PATH${NC}"
echo ""

# Function to check if .NET is installed
check_dotnet() {
    if ! command -v dotnet &> /dev/null; then
        echo -e "${RED}Error: .NET SDK is not installed or not in PATH.${NC}"
        echo -e "${YELLOW}Please install .NET SDK and try again.${NC}"
        exit 1
    fi
    
    local dotnet_version=$(dotnet --version)
    echo -e "${GREEN}.NET SDK found: $dotnet_version${NC}"
}

# Function to check if project exists
check_project() {
    if [ ! -f "$API_PROJECT_PATH/FlowOrchestrator.EntitiesManagers.Api.csproj" ]; then
        echo -e "${RED}Error: API project not found at $API_PROJECT_PATH${NC}"
        echo -e "${YELLOW}Please ensure you're running this script from the correct directory.${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}API project found.${NC}"
}

# Function to start infrastructure services
start_infrastructure() {
    echo -e "${BLUE}Starting infrastructure services (MongoDB, RabbitMQ, etc.)...${NC}"
    cd "$SCRIPT_DIR"
    
    if ! docker info >/dev/null 2>&1; then
        echo -e "${RED}Error: Docker is not running. Please start Docker and try again.${NC}"
        exit 1
    fi
    
    docker-compose -f docker-compose.yml -f docker-compose.test.yml up -d mongodb rabbitmq otel-collector hazelcast
    
    echo -e "${GREEN}Infrastructure services started.${NC}"
    echo -e "${YELLOW}Waiting for services to be ready...${NC}"
    sleep 10
}

# Function to start the API
start_api() {
    echo -e "${BLUE}Starting EntitiesManager API locally...${NC}"
    cd "$API_PROJECT_PATH"
    
    echo -e "${YELLOW}API will be available at:${NC}"
    echo -e "${CYAN}http://localhost:5130${NC} (Development)"
    echo -e "${CYAN}https://localhost:7236${NC} (Development HTTPS)"
    echo ""
    echo -e "${YELLOW}Press Ctrl+C to stop the API${NC}"
    echo ""
    
    # Start the API in development mode
    dotnet run
}

# Function to show help
show_help() {
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  start       Start infrastructure and API (default)"
    echo "  infra       Start only infrastructure services"
    echo "  api         Start only the API (assumes infrastructure is running)"
    echo "  help        Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                    # Start everything"
    echo "  $0 infra             # Start only Docker infrastructure"
    echo "  $0 api               # Start only the API"
}

# Main execution
case "${1:-start}" in
    "start")
        check_dotnet
        check_project
        start_infrastructure
        start_api
        ;;
    "infra")
        start_infrastructure
        echo -e "${GREEN}Infrastructure services are running.${NC}"
        echo -e "${YELLOW}You can now start the API with: $0 api${NC}"
        ;;
    "api")
        check_dotnet
        check_project
        start_api
        ;;
    "help"|"-h"|"--help")
        show_help
        ;;
    *)
        echo -e "${RED}Unknown command: $1${NC}"
        show_help
        exit 1
        ;;
esac
