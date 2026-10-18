"""Fail if the package depends on anything but Microsoft's MIT runtime packages.

The SDK is shipped to customers, so its dependencies must stay permissive
(MIT, BSD, Apache-2.0). Today the only one is System.Text.Json for
netstandard2.0, which pulls in a few System.* / Microsoft.Bcl.* packages.

Usage: dotnet list src/Esgo/Esgo.csproj package --include-transitive --format json \
         | python3 .github/scripts/check-dependencies.py
"""
import json
import sys

ALLOWED_PREFIXES = ("System.", "Microsoft.Bcl.", "Microsoft.NETCore.", "NETStandard.Library")

# utf-8-sig: PowerShell puts a BOM in front when piping to a native program.
report = json.loads(sys.stdin.buffer.read().decode("utf-8-sig"))
problems = []
for project in report.get("projects", []):
    for framework in project.get("frameworks", []):
        # Packages the SDK adds implicitly (e.g. Microsoft.NETFramework.ReferenceAssemblies for
        # net462) are build-time only and never end up in the .nupkg; the same goes for what
        # they pull in.
        build_only = tuple(p["id"] for p in framework.get("topLevelPackages", [])
                           if str(p.get("autoReferenced", "")).lower() == "true")
        for kind in ("topLevelPackages", "transitivePackages"):
            for package in framework.get(kind, []):
                if build_only and package["id"].startswith(build_only):
                    continue
                if not package["id"].startswith(ALLOWED_PREFIXES):
                    problems.append(f"{framework['framework']}: {package['id']} {package.get('resolvedVersion', '')}")

if problems:
    print("Dependencies outside the allow-list (check the license, then extend ALLOWED_PREFIXES):")
    print("\n".join("  " + p for p in problems))
    sys.exit(1)
print("Dependencies ok: only Microsoft runtime packages.")
