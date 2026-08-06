import json
import os
import sys

repo = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # tools -> repo root
sys.path.insert(0, repo)

import pygame
from UstViz import USTParser, NoteRenderer, SequenceGenerator

parser = USTParser()
parser.parse_file(os.path.join(repo, 'Test', '热异常.ust'))
renderer = NoteRenderer()
gen = SequenceGenerator()
gen.ust_parser = parser
gen.renderer = renderer

base = {
    'width': 640, 'height': 360, 'fps': 30,
    'note_color': (255, 0, 0), 'active_note_color': (0, 255, 0),
    'lyric_color': (255, 255, 255), 'background_color': (0, 0, 0),
    'judgment_line_color': (255, 255, 0), 'judgment_line_position': 0.2,
    'scroll_speed': 500, 'font_path': '', 'font_size': 24, 'fallback_font': 'simsun',
    'note_height': 20, 'note_corner_radius': 5, 'note_shadow': True,
    'transparent_background': False, 'lyric_offset': 15, 'fade_duration': 1.0,
    'show_lyric': True, 'show_pitch_curve': True,
    'pitch_curve_color': (0, 255, 255), 'pitch_curve_width': 3,
    'pitch_curve_shadow': True, 'pitch_curve_dots': True, 'pitch_curve_dot_size': 5,
    'pitch_curve_smoothness': 50, 'vertical_offset': 0,
}

full = dict(base)
simple = dict(base)
simple.update({'note_corner_radius': 0, 'note_shadow': False, 'show_lyric': False, 'show_pitch_curve': False})

pygame.init()
font = pygame.font.SysFont(base['fallback_font'], base['font_size'])
W, H = base['width'], base['height']
fps = base['fps']

out_dir = os.path.join(repo, 'Test', 'python_frames')
os.makedirs(out_dir, exist_ok=True)

# t = 1, 2, 4, 8 秒（避开淡入淡出区，alpha=255）
frames = [30, 60, 120, 240]
snapshot = {}


def render_one(config, current_time, prefix):
    pps = config['scroll_speed']
    judgment_x = W * config['judgment_line_position']
    lead_in = W / pps
    total_duration = parser.total_duration + lead_in + lead_in
    screen = pygame.Surface((W, H))
    screen.fill(config['background_color'])
    pygame.draw.line(screen, config['judgment_line_color'], (judgment_x, 0), (judgment_x, H), 2)
    visible = []
    for note in parser.notes:
        note_start_x = W + (note['start_time'] - current_time + lead_in) * pps
        note_end_x = W + (note['end_time'] - current_time + lead_in) * pps
        if note_end_x < 0 or note_start_x > W:
            continue
        if note['lyric'].upper() == 'R' or note['note_num'] <= 0:
            continue
        note_width = max(10, note_end_x - note_start_x)
        if note_width < 5:
            continue
        note_y = renderer.get_note_y_position(note['note_num'], H, config['vertical_offset'])
        is_active = note_start_x <= judgment_x <= note_end_x
        fade_alpha = 255
        if current_time < config['fade_duration']:
            fade_alpha = int(255 * (current_time / config['fade_duration']))
        elif current_time > total_duration - config['fade_duration']:
            fade_alpha = int(255 * ((total_duration - current_time) / config['fade_duration']))
        color = config['active_note_color'] if is_active else config['note_color']
        visible.append({
            'number': note['number'],
            'start_x': note_start_x, 'end_x': note_end_x, 'y': note_y,
            'is_active': is_active, 'alpha': fade_alpha,
            'r': color[0], 'g': color[1], 'b': color[2],
        })
        gen._draw_note(screen, note, current_time, config, pps, judgment_x, font, total_duration, lead_in)
    if config['show_pitch_curve']:
        gen._draw_pitch_curves(screen, parser.notes, current_time, config, pps, judgment_x, total_duration, lead_in)
    pygame.image.save(screen, os.path.join(out_dir, f'{prefix}_{frame_num}.png'))
    return visible


for frame_num in frames:
    current_time = frame_num / fps
    vis = render_one(full, current_time, 'frame')
    render_one(simple, current_time, 'simple')
    snapshot[str(frame_num)] = vis

with open(os.path.join(repo, 'Test', 'python_frame_snapshot.json'), 'w', encoding='utf-8') as f:
    json.dump(snapshot, f, ensure_ascii=False, indent=1)

print('frames written:', [f'{p}_{n}.png' for p in ('frame', 'simple') for n in frames])
print('snapshot frames:', list(snapshot.keys()), '| visible notes per frame:', {k: len(v) for k, v in snapshot.items()})
