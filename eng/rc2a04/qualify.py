"""Local-only RC2A-04 qualification. Requires Python 3 and .NET 10; never publishes.

Usage: python eng/rc2a04/qualify.py --artifacts <new absolute directory>
Run from a clean committed Gagamba checkout. Consumers and all package/build caches
are placed outside it. The existing machine cache is an explicitly inventoried
input to the controlled feed, never a consumer restore fallback.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET
import zipfile


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--artifacts', required=True, type=Path)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    templates = Path(__file__).resolve().parent
    root = args.artifacts.resolve()
    if root == repo or repo in root.parents or root.exists():
        raise RuntimeError('Artifacts must be a new directory outside the repository')
    if subprocess.check_output(['git', 'diff', '--name-only', 'HEAD'], cwd=repo).strip():
        raise RuntimeError('Tracked changes: commit source before qualification')
    source = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=repo, text=True).strip()
    tree = subprocess.check_output(['git', 'rev-parse', 'HEAD^{tree}'], cwd=repo, text=True).strip()
    version = '0.1.0-rc2a.' + source
    root.mkdir(parents=True)
    logs = root / 'logs'; logs.mkdir()
    environment = os.environ.copy()
    environment.update(DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', NUGET_HTTP_CACHE_PATH=str(root / 'http-cache'))
    commands = []

    def run(label, command, cwd=repo, expected=0, cache=None):
        env = environment.copy()
        if cache:
            env['NUGET_PACKAGES'] = str(cache)
        result = subprocess.run([str(x) for x in command], cwd=cwd, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, encoding='utf-8', errors='replace')
        (logs / (label + '.log')).write_text(result.stdout, encoding='utf-8')
        commands.append(dict(label=label, command=[str(x) for x in command], exitCode=result.returncode))
        (root / 'commands.json').write_text(json.dumps(commands, indent=2), encoding='utf-8')
        print(label, 'exit', result.returncode, flush=True)
        if (expected == 0 and result.returncode != 0) or (expected != 0 and result.returncode == 0):
            raise RuntimeError(label + ': unexpected exit; see log')
        return result.stdout

    def config(directory, feed, cache):
        directory.mkdir(parents=True, exist_ok=True)
        path = directory / 'NuGet.Config'
        element = ET.Element('configuration')
        sources = ET.SubElement(element, 'packageSources'); ET.SubElement(sources, 'clear')
        ET.SubElement(sources, 'add', key='controlled-local', value=str(feed))
        settings = ET.SubElement(element, 'config')
        ET.SubElement(settings, 'add', key='globalPackagesFolder', value=str(cache))
        fallback = ET.SubElement(element, 'fallbackPackageFolders'); ET.SubElement(fallback, 'clear')
        ET.ElementTree(element).write(path, encoding='utf-8', xml_declaration=True)
        return path

    def project(directory, references, template):
        directory.mkdir(parents=True)
        document = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
        properties = ET.SubElement(document, 'PropertyGroup')
        for key, value in dict(OutputType='Exe', TargetFramework='net10.0', ImplicitUsings='enable', Nullable='enable', TreatWarningsAsErrors='true', NuGetAudit='false', RestoreFallbackFolders='').items():
            ET.SubElement(properties, key).text = value
        items = ET.SubElement(document, 'ItemGroup')
        for name, exact in references.items():
            ET.SubElement(items, 'PackageReference', Include=name, Version='[' + exact + ']')
        path = directory / (directory.name + '.csproj')
        ET.ElementTree(document).write(path, encoding='utf-8', xml_declaration=True)
        shutil.copyfile(templates / template, directory / 'Program.cs')
        return path

    run('environment', ['dotnet', '--info'])
    feed = root / 'feed'; feed.mkdir()
    build_feed = root / 'build-feed'; build_feed.mkdir()
    global_cache = Path.home() / '.nuget/packages'
    copied = []
    for package, dependency_version, target in [
        ('microsoft.sourcelink.github', '10.0.401', build_feed),
        ('microsoft.sourcelink.common', '10.0.401', build_feed),
        ('microsoft.build.tasks.git', '10.0.401', build_feed),
        ('system.io.hashing', '10.0.12', build_feed),
        ('penghou.hufu', '0.1.0-preview.11', feed),
    ]:
        original = global_cache / package / dependency_version / (package + '.' + dependency_version + '.nupkg')
        destination = target / original.name
        shutil.copyfile(original, destination)
        copied.append(dict(package=package, version=dependency_version, source='explicit machine-cache archive copy', sha256=sha(destination)))
    (root / 'feed-inputs.json').write_text(json.dumps(copied, indent=2), encoding='utf-8')
    build_cache = root / 'build-cache'
    build_config = config(root / 'build-config', build_feed, build_cache)
    build_output = root / 'build-output'
    properties = ['-p:Version=' + version, '-p:PackageVersion=' + version, '-p:RepositoryCommit=' + source, '-p:SourceRevisionId=' + source, '-p:ContinuousIntegrationBuild=true', '-p:NuGetAudit=false']
    runtime = repo / 'src/Gagamba.Runtime/Gagamba.Runtime.csproj'
    run('package-restore', ['dotnet', 'restore', runtime, '--configfile', build_config, '--packages', build_cache, '--artifacts-path', build_output, *properties], cache=build_cache)
    names = ['Gagamba.Execution', 'Gagamba.Execution.Windows', 'Gagamba.Execution.Linux', 'Gagamba.Execution.MacOS', 'Gagamba.Runtime']
    for name in names:
        run('pack-' + name, ['dotnet', 'pack', repo / ('src/' + name + '/' + name + '.csproj'), '-c', 'Release', '--no-restore', '--artifacts-path', build_output, '-o', feed, *properties], cache=build_cache)
    packages = []
    ns = {'n': 'http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd'}
    for path in sorted(feed.glob('Gagamba.*.*pkg')):
        with zipfile.ZipFile(path) as archive:
            files = archive.namelist()
            record = dict(file=path.name, sha256=sha(path), files=files)
            if path.suffix == '.nupkg':
                xml = ET.fromstring(archive.read(next(n for n in files if n.endswith('.nuspec'))))
                # Work across NuGet nuspec namespace versions.
                metadata = next(x for x in xml if x.tag.split('}')[-1] == 'metadata')
                fields = {x.tag.split('}')[-1]: x for x in metadata}
                name = fields['id'].text
                assert fields['version'].text == version
                assert fields['repository'].attrib['commit'] == source
                assert fields['license'].attrib['type'] == 'expression' and fields['license'].text == 'Apache-2.0'
                assert 'README.md' in files
                dependencies = [x.attrib for x in metadata.iter() if x.tag.split('}')[-1] == 'dependency']
                expected = [] if name == 'Gagamba.Execution' else (names[:-1] if name == 'Gagamba.Runtime' else ['Gagamba.Execution'])
                assert sorted(x['id'] for x in dependencies) == sorted(expected)
                assert all(x['version'] in (version, '[' + version + ', )') for x in dependencies), dependencies
                dlls = [n for n in files if n.endswith('.dll')]
                assert dlls == ['lib/net10.0/' + name + '.dll'], dlls
                assert not any(n.startswith(('runtimes/', 'build/', 'buildTransitive/', 'contentFiles/')) for n in files)
                record.update(id=name, version=version, repositoryCommit=source, dependencies=dependencies, dllSha256=hashlib.sha256(archive.read(dlls[0])).hexdigest(), license='Apache-2.0')
            packages.append(record)
    (root / 'packages.json').write_text(json.dumps(dict(sourceCommit=source, sourceTree=tree, version=version, packages=packages), indent=2), encoding='utf-8')

    graphs = []
    for label, references, template in [
        ('direct', {'Gagamba.Execution': version}, 'Consumer.cs'),
        ('mixed', {'Gagamba.Execution': version, 'Gagamba.Runtime': version, 'Penghou.Hufu': '0.1.0-preview.11'}, 'Mixed.cs'),
    ]:
        directory = root / label
        csproj = project(directory, references, template)
        cache = root / (label + '-cache')
        assert not cache.exists()
        nuget = config(directory, feed, cache)
        run(label + '-restore', ['dotnet', 'restore', csproj, '--configfile', nuget, '--packages', cache, '--force', '--no-http-cache'], cwd=directory, cache=cache)
        assets = json.loads((directory / 'obj/project.assets.json').read_text(encoding='utf-8'))
        assert len(assets['packageFolders']) == 1
        assert Path(next(iter(assets['packageFolders']))).resolve() == cache.resolve()
        libraries = {key: value['type'] for key, value in assets['libraries'].items()}
        expected = {'Gagamba.Execution/' + version} if label == 'direct' else {name + '/' + version for name in names} | {'Penghou.Hufu/0.1.0-preview.11'}
        assert set(libraries) == expected and all(t == 'package' for t in libraries.values()), libraries
        sources = []
        for metadata in cache.glob('*/*/.nupkg.metadata'):
            resolved_source = json.loads(metadata.read_text(encoding='utf-8'))['source']
            assert Path(resolved_source).resolve() == feed.resolve()
            sources.append(dict(package=str(metadata.parent.relative_to(cache)), source='controlled-local'))
        assert len(sources) == len(libraries)
        graphs.append(dict(scenario=label, references=references, libraries=libraries, resolvedSources=sources, isolatedCacheInitiallyAbsent=True, projectReferences=False))
        command = ['dotnet', 'run', '--project', csproj, '-c', 'Release', '--no-restore']
        if label == 'direct':
            vector_path = directory / 'vectors.json'
            shutil.copyfile(repo / 'docs/evidence/restricted-consumer-contract-rc2a-02a-windows/exports/vector-manifest.json', vector_path)
            command += ['--', vector_path]
        run(label + '-run', command, cwd=directory, cache=cache)
    (root / 'graphs.json').write_text(json.dumps(graphs, indent=2), encoding='utf-8')

    negative = root / 'negative'
    csproj = project(negative, {'Gagamba.Execution': version}, 'Consumer.cs')
    cache = root / 'negative-cache'
    nuget = config(negative, feed, cache)
    run('negative-restore', ['dotnet', 'restore', csproj, '--configfile', nuget, '--packages', cache], cwd=negative, cache=cache)
    probes = {
        'view-construction': ('_ = new RestrictedManifestView();', 'CS1729'),
        'view-setter': ('RestrictedManifestView v = null!; v.SchemaVersion = 1;', 'CS0200'),
        'codec-launch': ('RestrictedManifestCodec.Launch();', 'CS0117'),
        'reference-release': ('RestrictedPreparationReference r = null!; r.Release();', 'CS1061'),
    }
    for label, (code, diagnostic) in probes.items():
        (negative / 'Program.cs').write_text('using Gagamba.Execution.Restricted.Contracts;\n' + code, encoding='utf-8')
        output = run('negative-' + label, ['dotnet', 'build', csproj, '-c', 'Release', '--no-restore'], cwd=negative, cache=cache, expected=1)
        assert diagnostic in output, output

    inspector = root / 'inspector'
    csproj = project(inspector, {}, 'Inspect.cs')
    cache = root / 'inspector-cache'
    nuget = config(inspector, feed, cache)
    run('inspector-restore', ['dotnet', 'restore', csproj, '--configfile', nuget, '--packages', cache], cwd=inspector, cache=cache)
    extracted = root / 'inspect-inputs'; extracted.mkdir()
    for extension in ['nupkg', 'snupkg']:
        package = feed / ('Gagamba.Execution.' + version + '.' + extension)
        with zipfile.ZipFile(package) as archive:
            for filename in archive.namelist():
                if filename.endswith(('.dll', '.pdb')):
                    (extracted / Path(filename).name).write_bytes(archive.read(filename))
    output = run('api-inspection', ['dotnet', 'run', '--project', csproj, '-c', 'Release', '--no-restore', '--', extracted / 'Gagamba.Execution.dll', extracted / 'Gagamba.Execution.pdb', source], cwd=inspector, cache=cache)
    json.loads(output)  # Do not accept compiler chatter as an API receipt.
    (root / 'api.json').write_text(output, encoding='utf-8')
    (root / 'result.json').write_text(json.dumps(dict(result='PASS', sourceCommit=source, sourceTree=tree, version=version, restrictions=['Hufu Sandbox package unavailable; core package only', 'Two historical recipe-only vectors not independently materialized', 'No native provider activation', 'No binary reproducibility claim']), indent=2), encoding='utf-8')
    print('PASS', source, version, flush=True)


if __name__ == '__main__':
    main()
