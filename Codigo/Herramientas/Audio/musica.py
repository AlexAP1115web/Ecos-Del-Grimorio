# pistas de musica del juego (una por ala, menu, jefes y creditos)
import sys
from sintetizador import *

# patrones de arpegio
ARP_SUBE = [0, 1, 2, 3, 4, 3, 2, 1]
ARP_ARPA = [0, 2, 1, 3, 2, 4, 3, 5]
ARP_RAPIDO = [0, 1, 2, 4, 3, 2, 1, 2, 0, 1, 2, 4, 5, 4, 2, 1]
ARP_LENTO = [0, None, 2, None, 4, None, 3, None]


def menu():
    # "El grimorio": Re menor, tranquilo y misterioso
    s = Cancion(76, 24)
    intro = ['Dm', 'Bb', 'F', 'C']
    a = ['Dm', 'Bb', 'F', 'C', 'Dm', 'Bb', 'Gm', 'A', 'Dm', 'Bb', 'F', 'C', 'Gm', 'A', 'Dm', 'Dm']
    prog = intro + a + intro
    s.acordes(0, prog, cuerdas, 0.45, octava=3, rev=0.5)
    s.arpegio(0, prog, arpa, ARP_ARPA, 0.5, 0.5, octava=4, pan=-0.25)
    s.bajo(0, prog, bajo, [(0, 3.8, 0)], 0.6)
    s.melodia(4, """
        A4/1.5 D5/0.5 F5/1 E5/1  D5/1.5 C5/0.5 D5/2  C5/1 A4/1 C5/1 F5/1  E5/3 r/1
        A4/1.5 D5/0.5 F5/1 A5/1  G5/1.5 F5/0.5 D5/2  D5/1 G5/1 F5/1 E5/1  E5/2 C#5/2
        F5/1.5 E5/0.5 D5/1 A5/1  Bb5/2 A5/1 G5/1  A5/1.5 G5/0.5 F5/1 C5/1  E5/2 G5/2
        Bb5/1.5 A5/0.5 G5/1 D5/1  C#5/1 E5/1 A5/1 G5/1  F5/2 E5/1 D5/1  D5/4
    """, flauta, 0.9, pan=0.15, rev=0.45)
    s.melodia(20, "A5/2 F5/2  D5/2 F5/2  C5/2 A4/2  E5/4", celesta, 0.5, pan=0.3)
    return s


def nivel1():
    # ala de Aprendizaje: La menor / Do mayor, aventura ligera
    s = Cancion(100, 28)
    intro = ['Am', 'F', 'C', 'G']
    A = ['Am', 'F', 'C', 'G', 'Am', 'F', 'G', 'E']
    B = ['F', 'G', 'Em', 'Am', 'Dm', 'G', 'C', 'E']
    prog = intro + A + B + A
    s.acordes(0, prog, cuerdas, 0.3, octava=3, rev=0.4)
    s.arpegio(0, prog, arpa, ARP_SUBE, 0.5, 0.45, octava=4, pan=-0.3)
    s.bajo(0, prog, bajo, [(0, 1.5, 0), (1.5, 0.5, 7), (2, 1.5, 0), (3.5, 0.5, 12)], 0.65)
    s.bateria(4, 24, {'k': 'x.......x.x.....', 'c': '....x.......x...', 'm': 'x.x.x.x.x.x.x.x.'}, 0.55)
    melA = """
        E5/1 A5/1 G5/0.5 E5/0.5 C5/1  D5/1 F5/1 E5/1 C5/1  G4/1 C5/1 E5/1 G5/1  F5/1.5 E5/0.5 D5/2
        E5/1 A5/1 B5/0.5 A5/0.5 G5/1  A5/1 F5/1 E5/1 D5/1  B4/1 D5/1 G5/1 F5/1  E5/3 G#4/1
    """
    melB = """
        A4/1 C5/1 F5/1.5 E5/0.5  D5/1 B4/1 G4/2  G4/1 B4/1 E5/1.5 D5/0.5  C5/2 A4/2
        F5/1 E5/1 D5/1 A4/1  B4/1 D5/1 G5/1 F5/1  E5/1.5 D5/0.5 C5/1 G4/1  B4/2 G#4/2
    """
    s.melodia(4, melA, clarinete, 0.85, pan=0.15)
    s.melodia(12, melB, flauta, 0.85, pan=0.15)
    s.melodia(20, melA, flauta, 0.8, pan=0.15)
    s.melodia(20, melA, celesta, 0.35, pan=-0.2, transponer=12)
    return s


def nivel2():
    # ala de Fuego: Mi frigio, ritmico con tambores
    s = Cancion(126, 28)
    intro = ['Em', 'Em', 'F', 'Em']
    A = ['Em', 'Em', 'F', 'Em', 'Am', 'G', 'F', 'E']
    B = ['Am', 'Am', 'Em', 'Em', 'F', 'G', 'F', 'E']
    prog = intro + A + B + A
    s.bajo(0, prog, bajo_sintetico, [(i * 0.5, 0.45, 0 if i % 4 != 3 else 12) for i in range(8)], 0.7)
    s.acordes(4, prog[4:], cuerdas, 0.25, octava=3, rev=0.3)
    s.bateria(0, 28, {'T': 'X..x..x.X..x.x..', 't': '......x.......x.', 'k': 'x.......x.......', 'h': '..x...x...x...x.'}, 0.6)
    for c in range(4, 28):
        nombre = prog[c]
        for tt in (0, 1.5, 3):
            for m in acorde(nombre, 3):
                s.poner(metales(mtof(m), 0.3 * s.spb * 2), s.t(c, tt), 0.35, -0.2, 0.25)
    melA = """
        E5/0.5 F5/0.5 G5/1 F5/0.5 E5/0.5 B4/1  E5/0.5 F5/0.5 G5/0.5 A5/0.5 B5/2  C6/1 B5/0.5 A5/0.5 G5/1 F5/1  E5/3 r/1
        A5/1 C6/1 B5/0.5 A5/0.5 G5/1  G5/1 B5/1 A5/0.5 G5/0.5 F5/1  F5/1 A5/1 G5/0.5 F5/0.5 E5/1  G#5/2 E5/2
    """
    melB = """
        A4/1 E5/1 A5/2  G5/0.5 A5/0.5 G5/0.5 F5/0.5 E5/2  B4/1 E5/1 G5/2  F5/0.5 G5/0.5 F5/0.5 E5/0.5 D5/2
        C5/1 F5/1 A5/2  B5/1 A5/0.5 G5/0.5 D5/2  A5/1 G5/1 F5/1 E5/1  E5/2 G#5/1 B5/1
    """
    s.melodia(4, melA, sintetizador, 0.75, pan=0.1, rev=0.25)
    s.melodia(12, melB, metales, 0.6, pan=0.1, rev=0.3)
    s.melodia(20, melA, sintetizador, 0.75, pan=0.1, rev=0.25)
    s.melodia(20, melA, sintetizador, 0.3, pan=-0.3, rev=0.25, transponer=-12)
    return s


def nivel3():
    # ala de Hielo: Fa# menor, lento con campanas
    s = Cancion(70, 26)
    intro = ['F#m', 'D']
    A = ['F#m', 'D', 'A', 'E', 'Bm', 'D', 'C#sus4', 'C#']
    B = ['D', 'E', 'F#m', 'F#m', 'Bm', 'A', 'D', 'C#']
    prog = intro + A + B + A
    s.acordes(0, prog, cuerdas, 0.4, octava=3, rev=0.7)
    s.acordes(0, prog, coro, 0.18, octava=4, rev=0.8)
    s.bajo(0, prog, bajo, [(0, 3.9, 0)], 0.45, octava=1)
    s.arpegio(2, prog[2:], celesta, ARP_LENTO, 0.5, 0.35, octava=5, pan=-0.35, rev=0.6)
    s.bateria(10, 8, {'m': '..x...x...x...x.'}, 0.4)
    melA = """
        C#6/2 A5/1 B5/1  A5/2 F#5/2  E5/1 A5/1 C#6/1 B5/1  B5/3 r/1
        D6/2 C#6/1 B5/1  A5/2 F#5/1 A5/1  G#5/2 F#5/2  G#5/2 F5/2
    """
    melB = """
        F#5/1 A5/1 D6/2  E6/1.5 D6/0.5 B5/2  C#6/2 A5/1 F#5/1  G#5/1 A5/1 C#6/2
        B5/1.5 D6/0.5 F#6/2  E6/1 C#6/1 A5/2  F#5/1 A5/1 B5/1 D6/1  C#6/4
    """
    s.melodia(2, melA, campana, 0.8, pan=0.2, rev=0.7)
    s.melodia(10, melB, campana, 0.8, pan=0.2, rev=0.7)
    s.melodia(10, melB, flauta, 0.35, pan=-0.1, rev=0.6, transponer=-12)
    s.melodia(18, melA, flauta, 0.6, pan=0.1, rev=0.6, transponer=-12)
    s.melodia(18, melA, celesta, 0.4, pan=0.3, rev=0.7)
    return s


def nivel4():
    # ala de Viento: Sol mayor, brillante y con movimiento
    s = Cancion(112, 28)
    intro = ['G', 'F', 'C', 'G']
    A = ['G', 'F', 'C', 'G', 'Em', 'C', 'D', 'D']
    B = ['C', 'D', 'Bm', 'Em', 'C', 'D', 'G', 'G']
    prog = intro + A + B + A
    s.acordes(0, prog, cuerdas, 0.28, octava=3, rev=0.45)
    s.arpegio(0, prog, arpa, ARP_RAPIDO, 0.25, 0.35, octava=4, pan=0.35)
    s.bajo(0, prog, bajo, [(0, 0.9, 0), (1.5, 0.4, 0), (2, 0.9, 7), (3, 0.9, 12)], 0.6)
    s.bateria(4, 24, {'k': 'x.....x...x.....', 's': '....x.......x...', 'h': 'x.x.x.x.x.x.x.x.', 'o': '..............x.'}, 0.5)
    melA = """
        D5/0.5 G5/0.5 A5/0.5 B5/0.5 D6/1 B5/1  C6/1 A5/1 F5/1 A5/1  G5/1.5 E5/0.5 C5/1 E5/1  D5/3 r/1
        E5/0.5 G5/0.5 B5/1 A5/0.5 G5/0.5 E5/1  G5/1 E5/1 C6/1.5 B5/0.5  A5/1 F#5/1 D5/1 E5/0.5 F#5/0.5  A5/4
    """
    melB = """
        E5/1 G5/1 C6/1 B5/1  A5/1 F#5/1 D6/2  B5/1 A5/0.5 G5/0.5 F#5/1 D5/1  E5/2 G5/1 B5/1
        C6/1.5 B5/0.5 A5/1 G5/1  F#5/1 A5/1 D6/1 C6/1  B5/1.5 A5/0.5 G5/1 D5/1  G5/4
    """
    s.melodia(4, melA, flauta, 0.9, pan=-0.1)
    s.melodia(12, melB, flauta, 0.9, pan=-0.1)
    s.melodia(12, melB, clarinete, 0.35, pan=0.2, transponer=-12)
    s.melodia(20, melA, flauta, 0.9, pan=-0.1)
    s.melodia(20, melA, celesta, 0.3, pan=0.3, transponer=12)
    return s


def nivel5():
    # corazon del Grimorio: Do menor, oscuro y misterioso
    s = Cancion(88, 28)
    intro = ['Cm', 'Ab', 'Fm', 'G']
    A = ['Cm', 'Ab', 'Fm', 'G', 'Cm', 'Bb', 'Ab', 'G']
    B = ['Fm', 'Cm', 'Ab', 'Bb', 'Fm', 'G', 'Cm', 'G']
    prog = intro + A + B + A
    s.acordes(0, prog, coro, 0.35, octava=3, rev=0.7)
    s.acordes(0, prog, cuerdas, 0.25, octava=2, rev=0.5, pan=0.0)
    s.bajo(0, prog, bajo_sintetico, [(i * 0.5, 0.4, 0) for i in range(8)], 0.55, octava=1)
    s.arpegio(0, prog, celesta, [0, 2, 4, 2, 3, 2, 4, 5], 0.5, 0.3, octava=5, pan=-0.3, rev=0.6)
    s.bateria(4, 24, {'b': 'X...............', 'T': '........x.....x.'}, 0.55)
    melA = """
        G4/2 C5/1 Eb5/1  Eb5/1.5 D5/0.5 C5/2  F5/1 Ab5/1 G5/1 F5/1  D5/3 B4/1
        G5/2 Eb5/1 C5/1  D5/1 F5/1 Bb5/2  Ab5/1 G5/1 F5/1 Eb5/1  D5/2 B4/1 D5/1
    """
    melB = """
        C5/1 F5/1 Ab5/2  G5/1.5 F5/0.5 Eb5/2  Eb5/1 Ab5/1 C6/2  Bb5/1.5 Ab5/0.5 G5/1 F5/1
        Ab5/2 G5/1 F5/1  G5/1 F5/1 D5/1 B4/1  C5/2 Eb5/1 G5/1  B4/2 D5/2
    """
    s.melodia(4, melA, cuerdas, 0.9, pan=0.15, rev=0.5)
    s.melodia(12, melB, cuerdas, 0.9, pan=0.15, rev=0.5)
    s.melodia(12, melB, flauta, 0.4, pan=-0.15, rev=0.5)
    s.melodia(20, melA, flauta, 0.7, pan=0.15, rev=0.5)
    s.melodia(20, melA, campana, 0.35, pan=-0.3, rev=0.6, transponer=12)
    return s


def jefe():
    # combate contra los guardianes: Re menor, rapido e intenso
    s = Cancion(144, 26)
    intro = ['Dm', 'Dm']
    A = ['Dm', 'Bb', 'C', 'A', 'Dm', 'Bb', 'Gm', 'A']
    B = ['Gm', 'Dm', 'Bb', 'A', 'Gm', 'Dm', 'Eb', 'A']
    prog = intro + A + B + A
    s.bajo(0, prog, bajo_sintetico, [(i * 0.5, 0.45, 12 if i % 2 else 0) for i in range(8)], 0.7)
    s.bateria(0, 2, {'T': 'X.x.X.x.X.x.XxXx', 't': '..x...x...x.x.x.'}, 0.65)
    s.bateria(2, 24, {'k': 'X.x...x.X.x...x.', 's': '....X.......X...', 'h': 'x.x.x.x.x.x.x.x.', 'T': '..............x.'}, 0.6)
    for c in range(2, 26):
        for tt in (0, 0.75, 1.5, 2.5, 3.0):
            for m in acorde(prog[c], 3):
                s.poner(metales(mtof(m), 0.22 * s.spb * 2), s.t(c, tt), 0.3, -0.2, 0.2)
    s.arpegio(2, prog[2:], ostinato, [0, 1, 2, 1, 3, 2, 1, 2] * 2, 0.25, 0.4, octava=4, pan=0.35, rev=0.2)
    melA = """
        D5/0.5 D5/0.5 F5/0.5 A5/0.5 D6/1 C6/0.5 A5/0.5  Bb5/1 A5/0.5 G5/0.5 F5/1 D5/1  E5/0.5 G5/0.5 C6/1 Bb5/0.5 A5/0.5 G5/1  A5/2 C#6/1 E6/1
        F6/1 E6/0.5 D6/0.5 A5/1 F5/1  G5/0.5 A5/0.5 Bb5/1 D6/1 Bb5/1  G5/1 Bb5/1 A5/0.5 G5/0.5 F5/1  E5/2 C#5/2
    """
    melB = """
        D6/1.5 C6/0.5 Bb5/1 G5/1  A5/1.5 G5/0.5 F5/1 D5/1  F5/1 Bb5/1 D6/1 F6/1  E6/2 C#6/2
        G5/0.5 A5/0.5 Bb5/0.5 C6/0.5 D6/2  F6/1 E6/0.5 D6/0.5 C6/1 A5/1  G5/1 Bb5/1 Eb6/1 D6/1  C#6/2 A5/1 E5/1
    """
    s.melodia(2, melA, sintetizador, 0.7, pan=0.1, rev=0.2, transponer=-12)
    s.melodia(10, melB, metales, 0.55, pan=0.1, rev=0.25, transponer=-12)
    s.melodia(18, melA, sintetizador, 0.7, pan=0.1, rev=0.2, transponer=-12)
    return s


def jefe_final():
    # eco de la Archimaga Elenora: Si menor, epico con coro
    s = Cancion(150, 26)
    intro = ['Bm', 'Bm']
    A = ['Bm', 'G', 'Em', 'F#', 'Bm', 'D', 'A', 'F#']
    B = ['G', 'A', 'Bm', 'Bm', 'G', 'A', 'F#', 'F#']
    prog = intro + A + B + A
    s.acordes(0, prog, coro, 0.45, octava=3, rev=0.6)
    s.bajo(0, prog, bajo_sintetico, [(i * 0.5, 0.45, 0) for i in range(8)], 0.7, octava=1)
    s.arpegio(0, prog, ostinato, [0, 1, 2, 3, 2, 1, 2, 3] * 2, 0.25, 0.45, octava=4, pan=-0.3, rev=0.2)
    s.bateria(0, 26, {'b': 'X.......X.......', 'T': '....x.x.....x.xx'}, 0.6)
    s.bateria(2, 24, {'k': 'x...x...x...x...', 's': '....X.......X...', 'h': '..x...x...x...x.'}, 0.55)
    melA = """
        F#5/1.5 B5/0.5 D6/1 C#6/1  B5/1 G5/1 D5/2  E5/1 G5/1 B5/1 A5/1  A#5/2 F#5/2
        B5/1 D6/1 F#6/1.5 E6/0.5  D6/1 A5/1 F#5/2  E5/0.5 F#5/0.5 A5/1 C#6/1 E6/1  C#6/4
    """
    melB = """
        G5/1 B5/1 D6/1.5 C#6/0.5  C#6/1 A5/1 E6/2  D6/1.5 C#6/0.5 B5/1 F#5/1  B5/4
        D6/1 E6/1 F#6/1 G6/1  E6/1.5 D6/0.5 C#6/1 A5/1  A#5/1 C#6/1 F#6/2  E6/1 C#6/1 A#5/2
    """
    s.melodia(2, melA, metales, 0.65, pan=0.1, rev=0.3, transponer=-12)
    s.melodia(10, melB, metales, 0.65, pan=0.1, rev=0.3, transponer=-12)
    s.melodia(10, melB, cuerdas, 0.5, pan=-0.2, rev=0.4)
    s.melodia(18, melA, metales, 0.65, pan=0.1, rev=0.3, transponer=-12)
    s.melodia(18, melA, cuerdas, 0.45, pan=-0.2, rev=0.4)
    return s


def creditos():
    # creditos: Fa mayor, calido (el tema del menu en modo mayor)
    s = Cancion(84, 28)
    intro = ['F', 'C', 'Dm', 'Bb']
    A = ['F', 'C', 'Dm', 'Bb', 'F', 'C', 'Bb', 'C']
    B = ['Dm', 'Bb', 'F', 'C', 'Bb', 'C', 'F', 'F']
    prog = intro + A + B + A
    s.acordes(0, prog, cuerdas, 0.4, octava=3, rev=0.55)
    s.arpegio(0, prog, arpa, ARP_ARPA, 0.5, 0.45, octava=4, pan=-0.3)
    s.bajo(0, prog, bajo, [(0, 2.8, 0), (3, 0.9, 7)], 0.55)
    melA = """
        C5/1.5 F5/0.5 A5/1 G5/1  G5/1.5 F5/0.5 E5/2  D5/1 F5/1 A5/1 D6/1  C6/2 Bb5/1 A5/1
        A5/1.5 G5/0.5 F5/1 C5/1  E5/1 G5/1 C6/1 Bb5/1  A5/1 F5/1 D5/1 F5/1  G5/3 r/1
    """
    melB = """
        A5/1.5 G5/0.5 F5/1 D5/1  F5/1 Bb5/1 D6/2  C6/1.5 A5/0.5 F5/1 A5/1  G5/2 E5/2
        D5/1 F5/1 Bb5/1 A5/1  G5/1 E5/1 C5/1 E5/1  F5/2 A5/1 G5/1  F5/4
    """
    s.melodia(4, melA, flauta, 0.85, pan=0.15, rev=0.45)
    s.melodia(12, melB, clarinete, 0.85, pan=0.15, rev=0.45)
    s.melodia(20, melA, flauta, 0.8, pan=0.15, rev=0.45)
    s.melodia(20, melA, celesta, 0.35, pan=-0.25, rev=0.5, transponer=12)
    return s


PISTAS = {
    'Menu': menu, 'Nivel1': nivel1, 'Nivel2': nivel2, 'Nivel3': nivel3, 'Nivel4': nivel4,
    'Nivel5': nivel5, 'Jefe': jefe, 'JefeFinal': jefe_final, 'Creditos': creditos,
}

if __name__ == '__main__':
    import wave
    destino = sys.argv[1]
    nombres = sys.argv[2:] or list(PISTAS)
    for nombre in nombres:
        datos = PISTAS[nombre]().render()
        pcm = (np.clip(datos, -1, 1) * 32767).astype(np.int16)
        with wave.open(f'{destino}/{nombre}.wav', 'wb') as w:
            w.setnchannels(2)
            w.setsampwidth(2)
            w.setframerate(SR)
            w.writeframes(pcm.tobytes())
        print(nombre, f'{len(datos) / SR:.1f} s')
