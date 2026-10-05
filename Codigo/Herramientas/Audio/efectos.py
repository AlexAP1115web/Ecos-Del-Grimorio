# Efectos de sonido del juego, en el mismo orden que el enum Sfx de AudioManager.cs
import sys
import wave
from sintetizador import *


def t_(seg):
    return np.arange(int(seg * SR)) / SR


def barrido(f0, f1, seg, forma='sin', curva=1.0):
    t = t_(seg)
    k = (t / seg) ** curva
    f = f0 + (f1 - f0) * k
    ph = np.cumsum(f) / SR
    if forma == 'sin':
        return np.sin(2 * np.pi * ph)
    if forma == 'tri':
        return 2 * np.abs(2 * (ph % 1) - 1) - 1
    return np.sign(np.sin(2 * np.pi * ph)) * 0.6


def caida(seg, rapidez):
    return np.exp(-t_(seg) * rapidez)


def ataque(seg, a=0.005):
    return np.clip(t_(seg) / a, 0, 1)


def mezclar(*partes):
    n = max(len(p) for p in partes)
    out = np.zeros(n)
    for p in partes:
        out[: len(p)] += p
    return out


def notas(lista, inst, paso):
    partes = []
    for i, m in enumerate(lista):
        y = inst(mtof(m), paso * 1.5)
        partes.append(np.concatenate([np.zeros(int(i * paso * SR)), y]))
    return mezclar(*partes)


def salto():
    y = barrido(280, 720, 0.13, 'tri', 0.6) * caida(0.13, 14) * ataque(0.13)
    return efecto(lp(y, 4000), 0.05)


def aterrizaje():
    y = lp(ruido(int(0.1 * SR)), 350) * caida(0.1, 35) * 2.5
    y = mezclar(y, np.sin(2 * np.pi * 85 * t_(0.1)) * caida(0.1, 30))
    return efecto(y, 0.02) * 0.7


def esquive():
    n = int(0.28 * SR)
    t = t_(0.28)
    y = ruido(n)
    a = bp(y, 600, 1600)
    b = bp(y, 1800, 4500)
    k = t / 0.28
    y = a * (1 - k) + b * k
    env = np.sin(np.pi * np.clip(t / 0.28, 0, 1)) ** 1.5
    return efecto(y * env, 0.1)


def arcano():
    t = t_(0.35)
    f = 600 + 350 * (t / 0.35) ** 0.5
    vib = 1 + 0.03 * np.sin(2 * np.pi * 28 * t)
    ph = np.cumsum(f * vib) / SR
    y = np.sin(2 * np.pi * ph) + 0.5 * np.sin(2 * np.pi * ph * 1.5) + 0.25 * np.sin(2 * np.pi * ph * 3.01)
    brillo = hp(ruido(len(t)), 6000) * 0.15
    return efecto((y + brillo) * caida(0.35, 9) * ataque(0.35, 0.01), 0.35)


def fuego():
    n = int(0.45 * SR)
    t = t_(0.45)
    soplo = lp(ruido(n), 1400) * np.sin(np.pi * np.clip(t / 0.45, 0, 1)) * 1.4
    chispas = np.zeros(n)
    for _ in range(25):
        i = rng.integers(0, n - 400)
        chispas[i:i + 300] += hp(ruido(300), 3000) * np.exp(-np.arange(300) / 40) * rng.uniform(0.3, 1)
    grave = np.sin(2 * np.pi * np.cumsum(140 - 60 * t / 0.45) / SR) * caida(0.45, 7) * 0.5
    return efecto(soplo + chispas * 0.5 + grave, 0.2)


def hielo():
    y = notas([midi('E6'), midi('B6'), midi('E7')], lambda f, d: campana(f, d, 1, 3.5, 1.5, 6), 0.045)
    n = len(y)
    siseo = hp(ruido(n), 5000) * np.exp(-np.arange(n) / SR * 8) * 0.25
    return efecto(y + siseo, 0.3)


def viento():
    n = int(0.5 * SR)
    t = t_(0.5)
    y = ruido(n)
    centro = 400 + 1600 * np.sin(np.pi * t / 0.5)
    partes = [bp(y, c * 0.7, c * 1.4) for c in (500, 1000, 1800)]
    k = np.sin(np.pi * t / 0.5)
    mezcla = partes[0] * (1 - k) + partes[1] * k * 0.8 + partes[2] * k ** 3 * 0.6
    return efecto(mezcla * np.sin(np.pi * np.clip(t / 0.5, 0, 1)) ** 1.2 + 0 * centro, 0.25)


def combo():
    golpe = bombo(1.0) * 1.2
    acorde_ = notas([midi('D5'), midi('A5'), midi('D6'), midi('F#6')], lambda f, d: campana(f, d, 1, 2.0, 2.0, 3), 0.03)
    n = int(0.6 * SR)
    soplo = bp(ruido(n), 300, 3000) * np.exp(-t_(0.6) * 5) * 0.5
    return efecto(mezclar(golpe, acorde_ * 0.8, soplo), 0.35)


def golpe_enemigo():
    n = int(0.14 * SR)
    y = bp(ruido(n), 500, 4000) * caida(0.14, 40)
    y = mezclar(y, np.sin(2 * np.pi * np.cumsum(190 - 110 * t_(0.14) / 0.14) / SR) * caida(0.14, 22) * 0.9)
    return efecto(y, 0.08)


def muerte_enemigo():
    y = barrido(650, 90, 0.55, 'sq', 0.7) * caida(0.55, 4) * 0.5
    y = lp(y, 2500)
    polvo = bp(ruido(int(0.55 * SR)), 800, 5000) * caida(0.55, 7) * 0.4
    return efecto(y + polvo, 0.35)


def dano_lira():
    y1 = barrido(260, 180, 0.12, 'sq') * caida(0.12, 12)
    y2 = barrido(200, 130, 0.14, 'sq') * caida(0.14, 12)
    y = mezclar(y1, np.concatenate([np.zeros(int(0.07 * SR)), y2]))
    y = lp(y, 2200) + bp(ruido(len(y)), 1000, 5000) * np.exp(-np.arange(len(y)) / SR * 25) * 0.4
    return efecto(y, 0.1)


def objeto():
    return efecto(notas([midi('B5'), midi('E6')], lambda f, d: campana(f, d, 1, 1.0, 0.6, 7), 0.07), 0.25)


def objeto_especial():
    return efecto(notas([midi(n) for n in ('C6', 'E6', 'G6', 'C7')], lambda f, d: campana(f, d, 1, 1.0, 0.9, 3.5), 0.08), 0.45)


def pagina():
    n = int(0.4 * SR)
    t = t_(0.4)
    roce = bp(ruido(n), 1800, 7000) * (0.5 + 0.5 * np.abs(np.sin(2 * np.pi * 9 * t))) * np.sin(np.pi * t / 0.4) * 0.6
    brillo = notas([midi('A6'), midi('E7')], lambda f, d: campana(f, d, 1, 1.0, 0.5, 5), 0.1)
    return efecto(mezclar(roce, np.concatenate([np.zeros(int(0.15 * SR)), brillo * 0.7])), 0.4)


def checkpoint():
    partes = [cuerdas(mtof(m), 0.6) for m in (midi('C4'), midi('G4'), midi('E5'))]
    y = mezclar(*partes)
    y = mezclar(y, notas([midi('G6'), midi('C7')], lambda f, d: campana(f, d, 1, 1.0, 0.8, 3), 0.12) * 0.6)
    return efecto(y, 0.5)


def cofre():
    t = t_(0.35)
    f = 120 + 40 * np.sin(2 * np.pi * 7 * t)
    chirrido = lp(saw(f, len(t)), 1800) * np.sin(np.pi * t / 0.35) * 0.4
    golpe = lp(ruido(int(0.1 * SR)), 500) * caida(0.1, 30)
    brillo = notas([midi(n) for n in ('E6', 'G#6', 'B6', 'E7')], lambda f, d: campana(f, d, 1, 1.0, 0.8, 3), 0.07)
    return efecto(mezclar(chirrido, np.concatenate([np.zeros(int(0.33 * SR)), golpe, ]),
                          np.concatenate([np.zeros(int(0.38 * SR)), brillo * 0.8])), 0.35)


def romper():
    partes = []
    for i in range(7):
        n = int(rng.uniform(0.05, 0.14) * SR)
        y = lp(ruido(n), rng.uniform(800, 3000)) * np.exp(-np.arange(n) / SR * 30)
        partes.append(np.concatenate([np.zeros(int(i * rng.uniform(0.03, 0.06) * SR)), y * rng.uniform(0.4, 1)]))
    golpe = lp(ruido(int(0.3 * SR)), 250) * caida(0.3, 10) * 2
    return efecto(mezclar(golpe, *partes), 0.25)


def menu_mover():
    return efecto(np.sin(2 * np.pi * 1250 * t_(0.045)) * caida(0.045, 70), 0.05) * 0.6


def menu_aceptar():
    return efecto(notas([midi('A5'), midi('E6')], lambda f, d: np.sin(2 * np.pi * f * t_(0.12)) * caida(0.12, 25), 0.06), 0.15)


def logro():
    arp = notas([midi(n) for n in ('G5', 'C6', 'E6', 'G6')], lambda f, d: metales(f, 0.12), 0.09)
    acorde_ = mezclar(*[metales(mtof(midi(n)), 0.6) for n in ('C5', 'E5', 'G5', 'C6')])
    campanas = notas([midi('C7'), midi('G7')], lambda f, d: campana(f, d, 1, 1.0, 0.8, 2.5), 0.1)
    y = mezclar(arp, np.concatenate([np.zeros(int(0.36 * SR)), acorde_ * 0.7]),
                np.concatenate([np.zeros(int(0.36 * SR)), campanas * 0.5]))
    return efecto(y, 0.45)


def portal():
    n = int(1.1 * SR)
    t = t_(1.1)
    sube = barrido(200, 1200, 1.1, 'sin', 2) * 0.3
    soplo = bp(ruido(n), 400, 3000) * (t / 1.1) * 0.5
    brillo = mezclar(*[campana(mtof(midi(x)), 0.8, 1, 1.0, 1.0, 2) for x in ('D6', 'F#6', 'A6')])
    y = mezclar((sube + soplo) * np.sin(np.pi * np.clip(t / 1.1, 0, 1)) ** 0.5,
                np.concatenate([np.zeros(int(0.7 * SR)), brillo * 0.6]))
    return efecto(y, 0.5)


def game_over():
    y = notas([midi('A4'), midi('F4'), midi('D4')], lambda f, d: mezclar(cuerdas(f, 0.55), campana(f, 0.6, 0.5, 1.0, 0.6, 2)), 0.45)
    return efecto(y, 0.5, 2.0)


def jefe_aparece():
    golpe = timbal(1.0, 55) * 1.5
    retumbe = lp(ruido(int(1.4 * SR)), 180) * caida(1.4, 2.5) * 1.5
    acorde_ = mezclar(*[metales(mtof(midi(n)), 0.9) for n in ('D3', 'A3', 'D4', 'F4')])
    return efecto(mezclar(golpe, retumbe, acorde_ * 0.8), 0.4)


def paso():
    n = int(0.07 * SR)
    t = t_(0.07)
    y = lp(ruido(n), 900) * np.exp(-t * 60) + np.sin(2 * np.pi * 110 * t) * np.exp(-t * 50) * 0.6
    return efecto(y, 0.0) * 0.5


EFECTOS = [
    ('Salto', salto), ('Aterrizaje', aterrizaje), ('Esquive', esquive),
    ('Arcano', arcano), ('Fuego', fuego), ('Hielo', hielo), ('Viento', viento), ('Combo', combo),
    ('GolpeEnemigo', golpe_enemigo), ('MuerteEnemigo', muerte_enemigo), ('DanoLira', dano_lira),
    ('Objeto', objeto), ('ObjetoEspecial', objeto_especial), ('Pagina', pagina), ('Checkpoint', checkpoint),
    ('Cofre', cofre), ('Romper', romper), ('MenuMover', menu_mover), ('MenuAceptar', menu_aceptar),
    ('Logro', logro), ('Portal', portal), ('GameOver', game_over), ('JefeAparece', jefe_aparece),
    ('Paso', paso),
]

if __name__ == '__main__':
    destino = sys.argv[1]
    for nombre, fn in EFECTOS:
        y = fn()
        pcm = (np.clip(y, -1, 1) * 32767).astype(np.int16)
        with wave.open(f'{destino}/{nombre}.wav', 'wb') as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(SR)
            w.writeframes(pcm.tobytes())
        print(f'{nombre}: {len(y) / SR:.2f} s')
