import hashlib, json, pathlib, subprocess
repo = pathlib.Path(r'C:\Users\Laszlos\source\repos\Gagamba')
out = pathlib.Path(__file__).parent
commit = 'af4300f5dff7e4e49fca98233911a44a15894a43'
paths = ['docs/plans/rc2b-06-b6-native-env-prep-01.md', 'docs/plans/rc2b-06-b6-native-qualification-matrix.md', 'docs/plans/rc2b-06-b6-native-provisioning-runbook.md', 'docs/evidence/rc2b-06-b6-native-env-prep-01.md']
dest = out / 'committed-documents'
dest.mkdir(exist_ok=True)
records = []
for i, path in enumerate(paths):
    blob = subprocess.check_output(['git', 'rev-parse', f'{commit}:{path}'], cwd=repo, text=True).strip()
    data = subprocess.check_output(['git', 'cat-file', 'blob', blob], cwd=repo)
    target = dest / f'{i+1:02d}-{pathlib.PurePosixPath(path).name}'
    if target.exists() and target.read_bytes() != data: raise RuntimeError(f'collision: {target}')
    target.write_bytes(data)
    records.append(dict(path=path, git_blob=blob, sha256=hashlib.sha256(data).hexdigest(), bytes=len(data), copy=str(target.relative_to(out))))
(out / 'document-binding.json').write_text(json.dumps(dict(commit=commit, tree=subprocess.check_output(['git','rev-parse',commit+'^{tree}'], cwd=repo,text=True).strip(), documents=records), indent=2)+'\n', encoding='utf-8')
