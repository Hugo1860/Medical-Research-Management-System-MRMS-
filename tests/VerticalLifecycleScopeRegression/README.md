# Vertical lifecycle list and detail authorization regression

This executable invokes the actual controller with synthetic identities and project records against a fully migrated, **isolated** PostgreSQL instance. It deletes its fixtures in a `finally` block. It exercises six application endpoints, lifecycle lists, parent and stage access, cross-project detail IDs, uninitiated children, totals, pagination, and filters. Its connection string must target a throwaway test database; do not run it against a development or production database. The allow-fixtures switch is an explicit operator confirmation, not a way to identify an isolated database.

Example for the isolated Docker PostgreSQL container used in this audit (the database must already have the application migrations and sanitized seed data):

```bash
set -eu
scope_container=hrms-scope-db-1793348779-83128
[ "$(docker inspect -f '{{index .Config.Labels "scientificresearch.isolated-uat"}}' "$scope_container")" = true ]
scope_stage=$(mktemp -d /tmp/vertical-lifecycle-regression-XXXXXX)
trap 'rm -rf "$scope_stage"' EXIT
mkdir -p "$scope_stage/ScientificResearch" "$scope_stage/tests" "$scope_stage/packages"
rsync -a --exclude bin --exclude obj --exclude wwwroot ScientificResearch/ScientificResearch/ "$scope_stage/ScientificResearch/ScientificResearch/"
cp -R tests/VerticalLifecycleScopeRegression "$scope_stage/tests/"
docker run --rm --memory=2g --cpus=2 \
  -v "$scope_stage:/src" -v "$scope_stage/packages:/root/.nuget/packages" \
  -v "$PWD/deploy/nuget-local:/nuget:ro" -w /src --entrypoint sh scientificresearch:local-sdk -ec \
  'dotnet restore tests/VerticalLifecycleScopeRegression/VerticalLifecycleScopeRegression.csproj -p:UseAppHost=false -p:SelfContained=false --source /nuget --source https://api.nuget.org/v3/index.json --disable-parallel && dotnet build tests/VerticalLifecycleScopeRegression/VerticalLifecycleScopeRegression.csproj -c Release -p:UseAppHost=false --no-restore -clp:ErrorsOnly'
docker run --rm --ulimit core=0 --network "container:$scope_container" --memory=2g --cpus=2 \
  -v "$scope_stage:/src" -v "$scope_stage/packages:/root/.nuget/packages" -w /src \
  -e 'VERTICAL_LIFECYCLE_TEST_CONNECTION=Host=127.0.0.1;Port=5432;Database=ScientificResearch_Model;Username=postgres' \
  -e VERTICAL_LIFECYCLE_TEST_ALLOW_FIXTURES=YES --entrypoint dotnet scientificresearch:local-sdk \
  tests/VerticalLifecycleScopeRegression/bin/Release/net10.0/VerticalLifecycleScopeRegression.dll
```

The role/step fixtures exist only for this run. The tests do not exercise HTTP authentication, real user accounts, or client-side access. The list negative control in the audit log was run on an intentionally weakened **temporary copy** of the controller where the visibility predicate was replaced by `TRUE`; it is not a historical baseline commit.
