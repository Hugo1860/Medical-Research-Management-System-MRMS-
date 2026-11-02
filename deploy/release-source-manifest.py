#!/usr/bin/env python3
"""Bind a local release to its code, migrations and frontend inputs (no secrets)."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def snapshot():
    paths = {ROOT / name for name in ('.dockerignore', 'Dockerfile', 'Dockerfile.published', 'docker-compose.yml')}
    for folder, patterns in {
        'ScientificResearch/ScientificResearch': ['**/*.cs', '*.csproj', 'appsettings.json', 'wwwroot/**/*', 'log4net.xml', 'ScientificResearchApi.xml'],
        'frontend/src': ['**/*'],
        'frontend': ['package.json', 'pnpm-lock.yaml', 'index.html', 'vite.config.ts', 'tsconfig*.json'],
        'database/postgresql/migrations': ['*.sql'],
        'database/postgresql': ['*.sh'],
        'tests': ['**/*.cs', '**/*.csproj'],
        'deploy': ['*.sh', '*.py', 'Dockerfile.regression', 'tests/*.sql', 'nginx*.conf', 'nginx*.conf.example', 'nginx*.example'],
    }.items():
        for pattern in patterns:
            for path in (ROOT / folder).glob(pattern):
                if path.is_file() and not {'bin', 'obj', 'node_modules'}.intersection(path.parts):
                    paths.add(path)
    return {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(paths)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['write', 'check', 'seal'])
    parser.add_argument('manifest', type=Path)
    parser.add_argument('--artifact-root', type=Path)
    parser.add_argument('--require-artifacts', action='store_true')
    args = parser.parse_args()
    current = snapshot()
    if args.mode == 'write':
        args.manifest.write_text(json.dumps({'format': 1, 'inputs': current}, ensure_ascii=False, indent=2) + '\n')
        print(f'Recorded {len(current)} release inputs')
        return
    if not args.manifest.is_file():
        raise SystemExit('Release source manifest missing; run deploy/build-local-release.sh')
    manifest = json.loads(args.manifest.read_text())
    saved = manifest.get('inputs', {})
    changed = [p for p in sorted(set(saved) | set(current)) if saved.get(p) != current.get(p)]
    if changed:
        raise SystemExit('Release inputs changed; rebuild the release:\n' + '\n'.join(changed[:20]))
    artifact_root = args.artifact_root or args.manifest.parent
    if args.mode == 'seal':
        files = [p for p in artifact_root.rglob('*') if p.is_file() and p.resolve() != args.manifest.resolve()]
        manifest['artifacts'] = {str(p.relative_to(artifact_root)): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(files)}
        args.manifest.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n')
    artifacts = manifest.get('artifacts', {})
    if args.require_artifacts and not artifacts:
        raise SystemExit('Release artifact checksums missing; rebuild with deploy/build-local-release.sh')
    if artifacts:
        actual = {str(p.relative_to(artifact_root)) for p in artifact_root.rglob('*')
                  if p.is_file() and p.resolve() != args.manifest.resolve()}
        if actual != set(artifacts):
            raise SystemExit('Release artifact file set changed: ' + ', '.join(sorted(actual ^ set(artifacts))[:20]))
    for name, digest in artifacts.items():
        path = (artifact_root / name).resolve()
        if not path.is_relative_to(artifact_root.resolve()) or not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != digest:
            raise SystemExit(f'Release artifact missing or changed: {name}')
    print(f'Release inputs match: {len(current)} files')


if __name__ == '__main__':
    main()
