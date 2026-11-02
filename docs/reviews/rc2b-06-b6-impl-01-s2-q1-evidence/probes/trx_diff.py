import sys, xml.etree.ElementTree as ET

def names(path):
    t = ET.parse(path)
    return [r.get('testName') for r in t.getroot().iter() if r.tag.endswith('}UnitTestResult')]

a = names(sys.argv[1])
b = names(sys.argv[2])
sa, sb = set(a), set(b)
print("A count", len(a), "B count", len(b))
print("only in A:", len(sa - sb))
for n in sorted(sa - sb):
    print("  A:", n.encode('unicode_escape').decode())
print("only in B:", len(sb - sa))
for n in sorted(sb - sa):
    print("  B:", n.encode('unicode_escape').decode())
