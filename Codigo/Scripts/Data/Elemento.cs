// los cuatro elementos de los hechizos de Lira
public enum Elemento
{
    Arcano,
    Fuego,
    Hielo,
    Viento
}

// combos elementales que se forman al lanzar dos hechizos seguidos
public enum TipoCombo
{
    Ninguno,
    ExplosionArcana,   // Arcano + Fuego
    VaporCegador,      // Fuego + Hielo
    GranizoCortante,   // Hielo + Viento
    TormentaDeAscuas   // Fuego + Viento
}

public static class ElementoColor
{
    public static UnityEngine.Color Get(Elemento e)
    {
        switch (e)
        {
            case Elemento.Fuego: return new UnityEngine.Color(1f, 0.5f, 0.1f);
            case Elemento.Hielo: return new UnityEngine.Color(0.6f, 0.9f, 1f);
            case Elemento.Viento: return new UnityEngine.Color(0.6f, 1f, 0.7f);
            default: return new UnityEngine.Color(0.75f, 0.5f, 1f);
        }
    }
}
