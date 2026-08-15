#!/usr/bin/env bash
set -e

echo "================================================="
echo "       TicketHub - Code Coverage Pipeline        "
echo "================================================="

# 1. Restore local dotnet tools
echo -e "\n[1/4] Restoring .NET local tools..."
dotnet tool restore

# 2. Clean old test results
echo -e "\n[2/4] Cleaning previous TestResults..."
rm -rf ./TestResults

# 3. Run tests with Coverlet code coverage
echo -e "\n[3/4] Running bUnit test suite with Coverlet coverage..."
dotnet test "TicketHub.Tests.bUnit/TicketHub.Tests.bUnit.csproj" \
    --collect:"XPlat Code Coverage" \
    --settings "coverage.runsettings" \
    --results-directory "./TestResults" \
    --logger "console;verbosity=minimal"

# 4. Generate visual HTML Report using ReportGenerator
echo -e "\n[4/4] Generating HTML Code Coverage Report..."
REPORT_DIR="./TestResults/CoverageReport"

dotnet tool run reportgenerator \
    "-reports:./TestResults/**/coverage.cobertura.xml" \
    "-targetdir:$REPORT_DIR" \
    "-sourcedirs:./" \
    "-reporttypes:Html;TextSummary;Badges" \
    "-assemblyfilters:+TicketHub.Core;+TicketHub.Application;+TicketHub.Infrastructure;+TicketHub.Web;-*Tests*" \
    "-title:TicketHub Coverage Dashboard" \
    "-verbosity:Warning"

echo "================================================="
echo " Coverage Report generated successfully!"
echo " Location: $REPORT_DIR/index.html"
echo "================================================="

if [ -f "$REPORT_DIR/Summary.txt" ]; then
    cat "$REPORT_DIR/Summary.txt"
fi
