# sintetizador para la musica y los efectos del juego
# cada pista son acordes + melodia con instrumentos hechos por sintesis
import numpy as np
from scipy.signal import butter, sosfilt, fftconvolve

SR = 44100
rng = np.random.default_rng(11)

# ---------------------------------------------------------------- utilidades

NOTAS = {'C': 0, 'D': 2, 'E': 4, 'F': 5, 'G': 7, 'A': 9, 'B': 11}


def midi(nombre):
    # 'C#5' -> 73, 'Bb4' -> 70
    n = NOTAS[nombre[0]]
    i = 1
    while i < len(nombre) and nombre[i] in '#b':
        n += 1 if nombre[i] == '#' else -1
        i += 1
    octava = int(nombre[i:])
    return n + 12 * (octava + 1)


def mtof(m):
    return 440.0 * 2 ** ((m - 69) / 12)


CALIDADES = {
    '': [0, 4, 7], 'm': [0, 3, 7], '7': [0, 4, 7, 10], 'm7': [0, 3, 7, 10],
    'maj7': [0, 4, 7, 11], 'sus4': [0, 5, 7], 'sus2': [0, 2, 7], 'dim': [0, 3, 6],
}


def acorde(nombre, octava=3):
    raiz = nombre[0]
    i = 1
    if i < len(nombre) and nombre[i] in '#b':
        raiz += nombre[i]
        i += 1
    calidad = nombre[i:]
    base = midi(raiz + str(octava))
    return [base + x for x in CALIDADES[calidad]]


def lp(x, fc, orden=2):
    fc = min(fc, SR * 0.45)
    return sosfilt(butter(orden, fc, 'low', fs=SR, output='sos'), x)


def hp(x, fc, orden=2):
    return sosfilt(butter(orden, fc, 'high', fs=SR, output='sos'), x)


def bp(x, f1, f2, orden=2):
    return sosfilt(butter(orden, [f1, min(f2, SR * 0.45)], 'band', fs=SR, output='sos'), x)


def env(n, a, d, s, r, sostener):
    # aDSR: a, d, r en segundos; sostener = segundos hasta soltar la nota
    t = np.arange(n) / SR
    e = np.ones(n) * s
    e = np.where(t < a, t / max(a, 1e-4), e)
    m = (t >= a) & (t < a + d)
    e[m] = 1 - (1 - s) * (t[m] - a) / max(d, 1e-4)
    suelta = t >= sostener
    nivel = np.interp(sostener, t, e) if sostener < t[-1] else s
    e[suelta] = nivel * np.exp(-(t[suelta] - sostener) / max(r / 5, 1e-4))
    return e


def fase(f, n, vib=0.0, vib_hz=5.0, vib_retardo=0.15):
    t = np.arange(n) / SR
    if vib > 0:
        prof = np.clip((t - vib_retardo) / 0.3, 0, 1) * vib
        f = f * (1 + prof * np.sin(2 * np.pi * vib_hz * t))
    return np.cumsum(np.full(n, f) if np.isscalar(f) else f) / SR


def saw(f, n, **kw):
    ph = fase(f, n, **kw) % 1.0
    dt = (f if np.isscalar(f) else np.mean(f)) / SR
    y = 2 * ph - 1
    m = ph < dt
    t = ph[m] / dt
    y[m] -= t + t - t * t - 1
    m = ph > 1 - dt
    t = (ph[m] - 1) / dt
    y[m] -= t * t + t + t + 1
    return y


def ruido(n):
    return rng.standard_normal(n)


# ---------------------------------------------------------------- instrumentos
# cada instrumento devuelve para una frecuencia y duracion

def cuerdas(f, dur, vel=1.0, brillo=1800):
    n = int((dur + 1.4) * SR)
    y = sum(saw(f * d, n, vib=0.004, vib_hz=5.2) for d in (0.996, 1.0, 1.004)) / 3
    y = lp(y, brillo)
    return y * env(n, 0.35, 0.3, 0.8, 1.2, dur) * vel * 0.5


def coro(f, dur, vel=1.0):
    n = int((dur + 1.6) * SR)
    y = sum(saw(f * d, n, vib=0.005, vib_hz=4.5) for d in (0.993, 1.0, 1.007)) / 3
    y = 0.6 * bp(y, 550, 900) + 0.35 * bp(y, 1000, 1400) + 0.15 * lp(y, 400)
    return y * env(n, 0.5, 0.4, 0.85, 1.4, dur) * vel * 1.2


def arpa(f, dur, vel=1.0):
    n = int(min(dur + 1.5, 3.0) * SR)
    t = np.arange(n) / SR
    y = np.zeros(n)
    for k in range(1, 9):
        if f * k > 12000:
            break
        y += np.sin(2 * np.pi * f * k * t) * np.exp(-t * (1.5 + k * 1.3)) / k ** 1.3
    ataque = np.clip(t / 0.004, 0, 1)
    return y * ataque * vel * 0.55


def campana(f, dur, vel=1.0, razon=3.5, indice=2.5, caida=1.3):
    n = int(min(dur + 2.5, 4.0) * SR)
    t = np.arange(n) / SR
    mod = indice * np.exp(-t * 3) * np.sin(2 * np.pi * f * razon * t)
    y = np.sin(2 * np.pi * f * t + mod) * np.exp(-t * caida)
    y += 0.3 * np.sin(2 * np.pi * f * 2 * t) * np.exp(-t * caida * 2)
    return y * np.clip(t / 0.002, 0, 1) * vel * 0.35


def celesta(f, dur, vel=1.0):
    return campana(f, dur, vel, razon=4.0, indice=1.2, caida=2.2)


def flauta(f, dur, vel=1.0):
    n = int((dur + 0.4) * SR)
    ph = fase(f, n, vib=0.006, vib_hz=5.0, vib_retardo=0.2)
    y = np.sin(2 * np.pi * ph) + 0.25 * np.sin(4 * np.pi * ph) + 0.08 * np.sin(6 * np.pi * ph)
    aire = bp(ruido(n), f * 0.8, f * 3) * 0.06
    return (y + aire) * env(n, 0.06, 0.1, 0.85, 0.25, dur) * vel * 0.38


def clarinete(f, dur, vel=1.0):
    n = int((dur + 0.4) * SR)
    ph = fase(f, n, vib=0.004, vib_hz=5.5, vib_retardo=0.25)
    y = np.sin(2 * np.pi * ph) + 0.45 * np.sin(6 * np.pi * ph) + 0.2 * np.sin(10 * np.pi * ph) + 0.08 * np.sin(14 * np.pi * ph)
    return lp(y, 3500) * env(n, 0.04, 0.1, 0.8, 0.2, dur) * vel * 0.32


def sintetizador(f, dur, vel=1.0):
    n = int((dur + 0.3) * SR)
    y = 0.5 * saw(f, n, vib=0.006, vib_hz=6) + 0.5 * saw(f * 1.006, n, vib=0.006, vib_hz=6)
    t = np.arange(n) / SR
    y = 0.6 * lp(y, 2600) + 0.4 * lp(y, 900)
    return y * env(n, 0.01, 0.15, 0.75, 0.15, dur) * vel * 0.32 * (1 + 0 * t)


def metales(f, dur, vel=1.0):
    n = int((dur + 0.4) * SR)
    y = (saw(f, n) + saw(f * 1.003, n) + 0.5 * saw(f * 0.5, n)) / 2.5
    t = np.arange(n) / SR
    brillante = lp(y, 3000)
    oscuro = lp(y, 600)
    abre = np.clip(t / 0.08, 0, 1) * np.exp(-t * 3)
    y = oscuro * (1 - abre) + brillante * abre
    return y * env(n, 0.02, 0.2, 0.7, 0.25, dur) * vel * 0.45


def bajo(f, dur, vel=1.0):
    n = int((dur + 0.25) * SR)
    t = np.arange(n) / SR
    y = np.sin(2 * np.pi * f * t) + 0.35 * lp(saw(f, n), 500 + 800 * 0)
    return y * env(n, 0.005, 0.25, 0.65, 0.12, dur) * vel * 0.55


def bajo_sintetico(f, dur, vel=1.0):
    n = int((dur + 0.15) * SR)
    t = np.arange(n) / SR
    y = saw(f, n)
    corte = 200 + 1400 * np.exp(-t * 18)
    y = 0.5 * lp(y, 1300) * np.exp(-t * 18) + lp(y, 280) + 0.6 * np.sin(2 * np.pi * f * t)
    return y * env(n, 0.003, 0.15, 0.7, 0.06, dur) * vel * 0.42 + 0 * corte[:n]


def ostinato(f, dur, vel=1.0):
    n = int((dur + 0.15) * SR)
    t = np.arange(n) / SR
    y = lp(saw(f, n), 2200)
    return y * np.exp(-t * 9) * np.clip(t / 0.003, 0, 1) * vel * 0.3


# percusion

def bombo(vel=1.0):
    n = int(0.45 * SR)
    t = np.arange(n) / SR
    f = 45 + 95 * np.exp(-t * 28)
    y = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 9)
    y += lp(ruido(n), 3000) * np.exp(-t * 120) * 0.25
    return y * vel * 0.9


def caja(vel=1.0):
    n = int(0.3 * SR)
    t = np.arange(n) / SR
    y = bp(ruido(n), 1200, 7000) * np.exp(-t * 22) * 0.7
    y += np.sin(2 * np.pi * 190 * t) * np.exp(-t * 30) * 0.6
    return y * vel * 0.55


def aplauso(vel=1.0):
    n = int(0.25 * SR)
    t = np.arange(n) / SR
    e = np.exp(-t * 25) * (1 + 0.6 * (np.sin(2 * np.pi * 90 * t) > 0.6))
    return bp(ruido(n), 900, 4000) * e * vel * 0.4


def platillo(vel=1.0, abierto=False):
    n = int((0.35 if abierto else 0.07) * SR)
    t = np.arange(n) / SR
    return hp(ruido(n), 7000) * np.exp(-t * (9 if abierto else 60)) * vel * 0.28


def maraca(vel=1.0):
    n = int(0.09 * SR)
    t = np.arange(n) / SR
    e = np.clip(t / 0.015, 0, 1) * np.exp(-t * 45)
    return bp(ruido(n), 4500, 11000) * e * vel * 0.22


def tambor(vel=1.0, f0=110):
    n = int(0.7 * SR)
    t = np.arange(n) / SR
    f = f0 * 0.62 + f0 * 0.38 * np.exp(-t * 12)
    y = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 6)
    y += lp(ruido(n), 900) * np.exp(-t * 25) * 0.5
    return y * vel * 0.8


def timbal(vel=1.0, f0=80):
    n = int(1.2 * SR)
    t = np.arange(n) / SR
    y = np.sin(2 * np.pi * f0 * t) * np.exp(-t * 3) + 0.4 * np.sin(2 * np.pi * f0 * 1.5 * t) * np.exp(-t * 4)
    y += lp(ruido(n), 500) * np.exp(-t * 20) * 0.4
    return y * vel * 0.6


# ---------------------------------------------------------------- cancion

class Cancion:
    def __init__(self, bpm, compases):
        self.bpm = bpm
        self.spb = 60.0 / bpm
        self.compases = compases
        self.largo = int(compases * 4 * self.spb * SR)
        cola = int(4.0 * SR)
        self.L = np.zeros(self.largo + cola)
        self.R = np.zeros(self.largo + cola)
        self.reverb = np.zeros((2, self.largo + cola))

    def t(self, compas, tiempo=0.0):
        return (compas * 4 + tiempo) * self.spb

    def poner(self, y, seg, vol=1.0, pan=0.0, rev=0.25):
        i = int(seg * SR)
        if i >= len(self.L):
            return
        y = y[: len(self.L) - i] * vol
        l = np.cos((pan + 1) * np.pi / 4)
        r = np.sin((pan + 1) * np.pi / 4)
        self.L[i:i + len(y)] += y * l
        self.R[i:i + len(y)] += y * r
        self.reverb[0, i:i + len(y)] += y * l * rev
        self.reverb[1, i:i + len(y)] += y * r * rev

    # ---- capas

    def acordes(self, inicio, prog, inst, vol=1.0, octava=3, pan=0.0, rev=0.4, dur_compas=1):
        for k, nombre in enumerate(prog):
            for m in acorde(nombre, octava):
                self.poner(inst(mtof(m), dur_compas * 4 * self.spb * 0.98), self.t(inicio + k * dur_compas), vol, pan, rev)

    def arpegio(self, inicio, prog, inst, patron, paso=0.5, vol=1.0, octava=4, pan=0.0, rev=0.3):
        for k, nombre in enumerate(prog):
            notas = acorde(nombre, octava)
            notas = notas + [x + 12 for x in notas] + [x + 24 for x in notas]
            pasos = int(4 / paso)
            for j in range(pasos):
                idx = patron[j % len(patron)]
                if idx is None:
                    continue
                self.poner(inst(mtof(notas[idx]), paso * self.spb * 1.5), self.t(inicio + k, j * paso), vol, pan, rev)

    def bajo(self, inicio, prog, inst, ritmo, vol=1.0, octava=2, rev=0.1):
        # ritmo: lista de
        for k, nombre in enumerate(prog):
            raiz = acorde(nombre, octava)[0]
            for (tt, d, iv) in ritmo:
                self.poner(inst(mtof(raiz + iv), d * self.spb), self.t(inicio + k, tt), vol, 0.0, rev)

    def melodia(self, inicio, texto, inst, vol=1.0, pan=0.0, rev=0.35, transponer=0):
        pos = 0.0
        for token in texto.split():
            nota, dur = token.split('/')
            dur = float(dur)
            if nota != 'r':
                m = midi(nota) + transponer
                self.poner(inst(mtof(m), dur * self.spb * 0.95), self.t(inicio, pos), vol, pan, rev)
            pos += dur

    def bateria(self, inicio, compases, patrones, vol=1.0, swing=0.0):
        # patrones: {'k': 'x...x...', 's': '....x...', ...} en dieciseisavos
        sonidos = {
            'k': lambda v: bombo(v), 's': lambda v: caja(v), 'c': lambda v: aplauso(v),
            'h': lambda v: platillo(v), 'o': lambda v: platillo(v, True), 'm': lambda v: maraca(v),
            't': lambda v: tambor(v, 120), 'T': lambda v: tambor(v, 75), 'b': lambda v: timbal(v, 70),
        }
        pans = {'h': 0.3, 'o': 0.3, 'm': -0.3, 't': -0.2, 'T': 0.2}
        for c in range(compases):
            for clave, patron in patrones.items():
                pasos = len(patron)
                for j, ch in enumerate(patron):
                    if ch == '.':
                        continue
                    v = 1.0 if ch == 'X' else 0.7 if ch == 'x' else 0.45
                    tt = j * 4.0 / pasos
                    if swing and j % 2 == 1:
                        tt += swing * 4.0 / pasos
                    self.poner(sonidos[clave](v), self.t(inicio + c, tt), vol, pans.get(clave, 0.0), 0.12)

    # ---- mezcla final

    def render(self, rt=2.6, mezcla=0.35):
        n = int(rt * SR)
        t = np.arange(n) / SR
        decay = np.exp(-t * 6.9 / rt)
        irl = lp(ruido(n), 6000) * decay
        irr = lp(ruido(n), 6000) * decay
        irl /= np.sqrt(np.sum(irl ** 2))
        irr /= np.sqrt(np.sum(irr ** 2))
        wl = fftconvolve(self.reverb[0], irl)[: len(self.L)]
        wr = fftconvolve(self.reverb[1], irr)[: len(self.R)]
        L = self.L + wl * mezcla
        R = self.R + wr * mezcla
        # bucle sin cortes: lo que sobra al final se suma al principio
        Lf = L[: self.largo].copy()
        Rf = R[: self.largo].copy()
        cola = len(L) - self.largo
        Lf[:cola] += L[self.largo:]
        Rf[:cola] += R[self.largo:]
        st = np.stack([Lf, Rf], axis=1)
        st = hp(st.T, 30).T
        pico = np.max(np.abs(st))
        st = np.tanh(st / pico * 1.3) / np.tanh(1.3)
        return st * 0.89


def efecto(y, rev=0.2, rt=1.2):
    # normaliza un efecto mono y le agrega un poco de reverberacion
    n = int(rt * SR)
    t = np.arange(n) / SR
    ir = lp(ruido(n), 5000) * np.exp(-t * 6.9 / rt)
    ir /= np.sqrt(np.sum(ir ** 2))
    y = np.concatenate([y, np.zeros(int(0.05 * SR))])
    w = fftconvolve(y, ir)[: len(y) + n // 2]
    out = np.zeros(len(w))
    out[: len(y)] += y
    out += w * rev
    # recorta el silencio del final
    umbral = np.max(np.abs(out)) * 0.002
    fin = len(out) - np.argmax(np.abs(out[::-1]) > umbral)
    out = out[: fin + 200]
    out *= np.clip((len(out) - np.arange(len(out))) / 400, 0, 1)
    return out / np.max(np.abs(out)) * 0.85
