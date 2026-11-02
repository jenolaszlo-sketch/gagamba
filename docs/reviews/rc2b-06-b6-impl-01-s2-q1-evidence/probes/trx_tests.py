import sys, re, xml.etree.ElementTree as ET

def names(path):
    tree = ET.parse(path)
    root = tree.getroot()
    ns = {'t': root.tag.split('}')[0].strip('{')}
    out = []
    for r in root.iter():
        if r.tag.endswith('}UnitTestResult'):
            out.append((r.get('testName'), r.get('outcome')))
    return out

for p in sys.argv[1:]:
    n = names(p)
    print("==", p, "count", len(n))
    for name, outcome in n:
        print("  ", outcome, name)
