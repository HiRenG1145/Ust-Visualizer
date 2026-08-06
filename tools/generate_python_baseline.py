import json
import os
import re
import sys

repo = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # tools -> repo root
ust_path = os.path.join(repo, 'Test', '热异常.ust')
src_path = os.path.join(repo, 'UstViz.py')

with open(src_path, encoding='utf-8') as f:
    src = f.read()

# 仅提取 USTParser 类（只依赖标准库 re），避免导入 pygame
start = src.index('class USTParser:')
end = src.index('class NoteRenderer:')
ns = {'re': re}
exec(src[start:end], ns)
Parser = ns['USTParser']

parser = Parser()
ok = parser.parse_file(ust_path)
if not ok:
    print('PARSE FAILED')
    sys.exit(1)


def note_to_dict(n):
    return {
        'number': n['number'],
        'length': n['length'],
        'lyric': n['lyric'],
        'note_num': n['note_num'],
        'pbs': list(n['pbs']),
        'pbw': list(n['pbw']),
        'pby': list(n['pby']),
        'pbm': list(n['pbm']),
        'pitch_bend': list(n['pitch_bend']),
        'start_time': n['start_time'],
        'end_time': n['end_time'],
        'duration': n['duration'],
    }


result = {
    'tempo': parser.tempo,
    'project_name': parser.project_name,
    'total_duration': parser.total_duration,
    'note_count': len(parser.notes),
    'notes': [note_to_dict(n) for n in parser.notes],
    'pitch_curves': {},
}

curve_notes = 0
for i, n in enumerate(parser.notes):
    if n['lyric'].upper() == 'R' or n['note_num'] <= 0:
        continue
    pts = parser.calculate_pitch_curve(n, resolution=50)
    result['pitch_curves'][str(i)] = [[p[0], p[1]] for p in pts]
    curve_notes += 1

print('valid notes =', len(parser.notes), '| pitch curves =', curve_notes)

out = os.path.join(repo, 'Test', 'python_baseline.json')
with open(out, 'w', encoding='utf-8') as f:
    json.dump(result, f, ensure_ascii=False, indent=1)
print('written:', out)
print('size bytes:', os.path.getsize(out))
