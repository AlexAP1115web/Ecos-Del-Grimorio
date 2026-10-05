# voces de los personajes estilo RPG (silabas cortas) y quejidos de Lira
import sys
import wave
from scipy.signal import lfilter
from sintetizador import *

# formantes aproximados de las vocales del español (voz adulta media)
VOCALES = {
    'a': [(800, 90), (1250, 110), (2600, 160)],
    'e': [(480, 70), (1850, 100), (2600, 150)],
    'i': [(320, 60), (2250, 100), (3000, 160)],
    'o': [(520, 80), (900, 90), (2550, 150)],
    'u': [(350, 70), (750, 90), (2450, 150)],
}


def resonador(x, f, bw):
    # filtro resonante de dos polos (un formante)
    r = np.exp(-np.pi * bw / SR)
    th = 2 * np.pi * f / SR
    a = [1, -2 * r * np.cos(th), r * r]
    b = [1 - r]
    return lfilter(b, a, x)


def glotal(f0, n, jitter=0.01, respiro=0.05, aspereza=0.0):
    # fuente de voz: tren de pulsos suavizado con algo de aire
    t = np.arange(n) / SR
    f = f0 * (1 + jitter * np.sin(2 * np.pi * 5.5 * t) + jitter * 0.5 * rng.standard_normal(n).cumsum() / np.sqrt(np.arange(1, n + 1)))
    ph = np.cumsum(f) / SR % 1.0
    pulso = np.where(ph < 0.6, np.sin(np.pi * ph / 0.6) ** 2, 0.0)  # abre y cierra la glotis
    pulso = np.diff(pulso, prepend=0) * 40
    if aspereza:
        pulso *= 1 + aspereza * np.sign(np.sin(2 * np.pi * f0 * 0.5 * t))
    return pulso + respiro * ruido(n)


def vocal(v, f0, dur, escala=1.0, curva=0.0, respiro=0.05, aspereza=0.0, cierre=0.04):
    n = int(dur * SR)
    t = np.arange(n) / SR
    f0s = f0 * (1 + curva * (t / dur))  # entonacion que sube o baja
    fuente = glotal(f0s, n, respiro=respiro, aspereza=aspereza)
    y = np.zeros(n)
    for i, (f, bw) in enumerate(VOCALES[v]):
        y += resonador(fuente, f * escala, bw * escala) * (1.0, 0.6, 0.25)[i]
    env = np.clip(t / 0.012, 0, 1) * np.clip((dur - t) / cierre, 0, 1)
    return y * env


def consonante(tipo, escala=1.0):
    n = int(0.03 * SR)
    t = np.arange(n) / SR
    if tipo in 'tkp':
        return bp(ruido(n), 1500 * escala, 6000) * np.exp(-t * 120) * 0.35
    if tipo == 's':
        return bp(ruido(int(0.05 * SR)), 4000, 9000) * 0.18
    return np.zeros(0)


def silaba(c, v, f0, escala, curva, dur=0.1, **kw):
    pre = consonante(c, escala)
    y = vocal(v, f0, dur, escala, curva, **kw)
    if c in 'mnl':
        y[: int(0.025 * SR)] *= np.linspace(0.2, 1, int(0.025 * SR))  # entrada suave
    return np.concatenate([pre, y])


def normalizar(y, pico=0.8, lowcut=90):
    y = hp(y, lowcut)
    return y / (np.max(np.abs(y)) + 1e-9) * pico


# cada personaje: tono base (Hz), escala de formantes, aire y aspereza
PERSONAJES = {
    'Lira': dict(f0=330, escala=1.18, respiro=0.06, aspereza=0.0),
    'Sable': dict(f0=205, escala=1.08, respiro=0.08, aspereza=0.0),
    'Kaelor': dict(f0=105, escala=0.88, respiro=0.04, aspereza=0.25),
    'Isolde': dict(f0=245, escala=1.12, respiro=0.12, aspereza=0.0),
    'Threnody': dict(f0=140, escala=0.95, respiro=0.2, aspereza=0.0),
    'Elenora': dict(f0=215, escala=1.1, respiro=0.1, aspereza=0.0),
    'Eco': dict(f0=170, escala=1.0, respiro=0.6, aspereza=0.0),
}
SILABAS = [('l', 'a'), ('t', 'e'), ('m', 'i'), ('k', 'o'), ('n', 'a'), ('s', 'e'), ('p', 'o'), ('l', 'i')]


def eco(y, retardo=0.11, veces=3, caida=0.45):
    out = np.concatenate([y, np.zeros(int(retardo * SR * veces))])
    for k in range(1, veces + 1):
        i = int(retardo * SR * k)
        out[i:i + len(y)] += lp(y, 3000) * caida ** k
    return out


def voces_personaje(nombre):
    p = PERSONAJES[nombre]
    clips = []
    for j, (c, v) in enumerate(SILABAS):
        curva = rng.uniform(-0.12, 0.12)
        dur = rng.uniform(0.075, 0.11)
        f0 = p['f0'] * rng.uniform(0.94, 1.08)
        y = silaba(c, v, f0, p['escala'], curva, dur, respiro=p['respiro'], aspereza=p['aspereza'])
        if nombre == 'Elenora':
            y = eco(y)  # voz fantasmal con eco
            y = y + 0.4 * np.roll(y, int(0.012 * SR))  # coro ligero
        if nombre == 'Eco':
            y = eco(bp(y, 400, 5000), 0.09, 2, 0.5)  # susurro
        if nombre == 'Kaelor':
            y = np.tanh(y / np.max(np.abs(y)) * 2.0)  # voz mas ronca
        clips.append(normalizar(y, 0.75))
    return clips


def queja(f_ini, f_fin, dur, vocal_='a', escala=1.18, respiro=0.15, golpe=True):
    n = int(dur * SR)
    t = np.arange(n) / SR
    f0 = f_ini * (f_fin / f_ini) ** (t / dur)
    fuente = glotal(f0, n, jitter=0.02, respiro=respiro)
    y = np.zeros(n)
    for i, (f, bw) in enumerate(VOCALES[vocal_]):
        y += resonador(fuente, f * escala, bw * escala * 1.3) * (1.0, 0.6, 0.25)[i]
    env = np.clip(t / 0.008, 0, 1) * np.exp(-t * (3.0 / dur)) * np.clip((dur - t) / 0.06, 0, 1)
    y *= env
    if golpe:
        y = np.concatenate([bp(ruido(int(0.012 * SR)), 1500, 6000) * 0.3, y])
    return normalizar(y, 0.85)


def lira_dano():
    return [
        queja(470, 300, 0.24, 'a'),
        queja(520, 340, 0.2, 'e'),
        queja(440, 260, 0.28, 'a', respiro=0.25),
    ]


def lira_esfuerzo():
    return [
        queja(360, 420, 0.16, 'a', respiro=0.3, golpe=False),
        queja(380, 330, 0.14, 'e', respiro=0.3, golpe=False),
    ]


def lira_caida():
    y = queja(500, 200, 0.9, 'a', respiro=0.2)
    return [normalizar(np.concatenate([y, np.zeros(int(0.2 * SR))]) , 0.85)]


def guardar(y, ruta):
    pcm = (np.clip(y, -1, 1) * 32767).astype(np.int16)
    with wave.open(ruta, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())


if __name__ == '__main__':
    destino = sys.argv[1]
    for nombre in PERSONAJES:
        for i, y in enumerate(voces_personaje(nombre)):
            guardar(y, f'{destino}/Voz{nombre}_{i + 1}.wav')
    for i, y in enumerate(lira_dano()):
        guardar(y, f'{destino}/LiraDano_{i + 1}.wav')
    for i, y in enumerate(lira_esfuerzo()):
        guardar(y, f'{destino}/LiraEsfuerzo_{i + 1}.wav')
    guardar(lira_caida()[0], f'{destino}/LiraCaida.wav')
    print('listo')
