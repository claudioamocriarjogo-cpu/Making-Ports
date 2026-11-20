using UnityEngine;

// Controles na tela, estilo Winlator.
// Duas zonas separadas: o joystick fica na esquerda e a câmera (arrastar) na direita.
// Um toque que começa numa zona nunca passa para a outra.
// Só aparece no Android.
public class RevengeTouchLayer : MonoBehaviour
{
    // Fração da largura onde começa a zona da câmera. Tudo à esquerda dela é zona do joystick.
    const float InicioZonaCamera = 0.45f;

    class Botao
    {
        public string rotulo;
        public KeyCode tecla;
        public Rect r;            // em pixels, origem embaixo à esquerda (igual ao Input.touches)
        public int dedo = -1;
        public bool apertadoAntes;
    }

    Botao[] botoes;
    Texture2D anel;
    Texture2D disco;
    GUIStyle estiloRotulo;
    Vector2 centroJoy;
    float raioJoy;
    float unidade;
    int dedoJoy = -1;
    int dedoCamera = -1;
    bool correr; // Shift liga/desliga: um toque liga, o próximo desliga

    static bool criado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Criar()
    {
        if (criado || Application.platform != RuntimePlatform.Android) return;
        criado = true;
        AplicarQualidadeBaixa();
        GameObject go = new GameObject("RevengeTouchLayer");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<RevengeTouchLayer>();
    }

    // Celular fraco: qualidade Low, texturas na metade da resolução, sem sombras e 30 FPS
    static void AplicarQualidadeBaixa()
    {
        QualitySettings.SetQualityLevel(1, true);
        QualitySettings.masterTextureLimit = 1;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.pixelLightCount = 0;
        QualitySettings.antiAliasing = 0;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
    }

    void Awake()
    {
        anel = Circulo(128, 12f);
        disco = Circulo(128, 0f);
        // Ordem fixa: a posição de cada botão está em Layout()
        botoes = new[]
        {
            Novo("Ctrl", KeyCode.LeftControl),
            Novo("1", KeyCode.Alpha1),
            Novo("2", KeyCode.Alpha2),
            Novo("3", KeyCode.Alpha3),
            Novo("4", KeyCode.Alpha4),
            Novo("5", KeyCode.Alpha5),
            Novo("=", KeyCode.Equals),
            Novo("-", KeyCode.Minus),
            Novo("Tab", KeyCode.Tab),
            Novo("Enter", KeyCode.Return),
            Novo("Shift", KeyCode.LeftShift),
            Novo("F", KeyCode.F),
            Novo("C", KeyCode.C),
            Novo("E", KeyCode.E),
            Novo("Q", KeyCode.Q),
            Novo("R", KeyCode.R),
        };
        Layout();
    }

    static Botao Novo(string rotulo, KeyCode tecla)
    {
        return new Botao { rotulo = rotulo, tecla = tecla };
    }

    // Posições em frações da tela (medidas na imagem de referência).
    // Os tamanhos são em milímetros, para ficarem do mesmo tamanho físico em qualquer celular.
    void Layout()
    {
        float w = Screen.width;
        float h = Screen.height;
        float pxPorMm = (Screen.dpi > 0 ? Screen.dpi : 160f) / 25.4f;
        unidade = Mathf.Min(w, h) * 0.19f;

        float pequeno = Mathf.Clamp(unidade * 0.72f, 8f * pxPorMm, 12f * pxPorMm);
        float passoNumero = pequeno * 1.16f;
        float acao = Mathf.Clamp(h * 0.195f, 11f * pxPorMm, 15f * pxPorMm);
        float shift = Mathf.Clamp(unidade * 0.85f, 9f * pxPorMm, 13f * pxPorMm);

        raioJoy = Mathf.Clamp(h * 0.165f, 11f * pxPorMm, 15f * pxPorMm);
        centroJoy = new Vector2(w * 0.173f, h * 0.228f);

        botoes[0].r = new Rect(w * 0.018f, h * 0.835f, pequeno, pequeno);                  // Ctrl
        for (int i = 0; i < 5; i++)
            botoes[1 + i].r = new Rect(w * 0.273f + i * passoNumero, h * 0.835f, pequeno, pequeno); // 1 a 5
        botoes[6].r = new Rect(w * 0.747f, h * 0.865f, pequeno, pequeno);                  // =
        botoes[7].r = new Rect(w * 0.813f, h * 0.865f, pequeno, pequeno);                  // -
        botoes[8].r = new Rect(w * 0.914f, h * 0.842f, pequeno, pequeno);                  // Tab
        botoes[9].r = new Rect(w * 0.914f, h * 0.675f, pequeno, pequeno);                  // Enter
        botoes[10].r = new Rect(w * 0.012f, h * 0.422f, shift, shift);                     // Shift
        botoes[11].r = new Rect(w * 0.687f, h * 0.205f, acao, acao);                       // F
        botoes[12].r = new Rect(w * 0.812f, h * 0.205f, acao, acao);                       // C
        botoes[13].r = new Rect(w * 0.632f, h * 0.018f, acao, acao);                       // E
        botoes[14].r = new Rect(w * 0.755f, h * 0.018f, acao, acao);                       // Q
        botoes[15].r = new Rect(w * 0.878f, h * 0.018f, acao, acao);                       // R
    }

    void Update()
    {
        Layout();

        Touch[] toques = Input.touches;
        if (toques.Length == 0) SoltarTudo();
        foreach (Touch t in toques)
        {
            if (t.phase == TouchPhase.Began) Iniciar(t);
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) Terminar(t.fingerId);
            else Segurar(t);
        }
        if (dedoJoy < 0) TouchInput.joystick = Vector2.zero;

        foreach (Botao b in botoes)
        {
            bool agora = b.dedo >= 0;
            if (b.tecla == KeyCode.LeftShift)
            {
                if (agora && !b.apertadoAntes) correr = !correr;
                TouchInput.Pressionar(b.tecla, correr);
            }
            else
            {
                TouchInput.Pressionar(b.tecla, agora);
            }
            b.apertadoAntes = agora;
        }

        // O joystick também aciona W A S D, porque o jogo lê essas teclas para andar
        Vector2 j = TouchInput.joystick;
        TouchInput.Pressionar(KeyCode.W, j.y > 0.5f);
        TouchInput.Pressionar(KeyCode.S, j.y < -0.5f);
        TouchInput.Pressionar(KeyCode.A, j.x < -0.5f);
        TouchInput.Pressionar(KeyCode.D, j.x > 0.5f);
    }

    // Ordem: botões primeiro; depois joystick (só na zona dele); depois câmera (só na zona dela)
    void Iniciar(Touch t)
    {
        Vector2 p = t.position;
        float folga = unidade * 0.1f;
        foreach (Botao b in botoes)
        {
            Rect area = new Rect(b.r.x - folga, b.r.y - folga, b.r.width + folga * 2f, b.r.height + folga * 2f);
            if (b.dedo < 0 && area.Contains(p))
            {
                b.dedo = t.fingerId;
                return;
            }
        }
        if (dedoJoy < 0 && p.x < Screen.width * InicioZonaCamera && (p - centroJoy).magnitude < raioJoy * 1.8f)
        {
            dedoJoy = t.fingerId;
            return;
        }
        if (dedoCamera < 0 && p.x >= Screen.width * InicioZonaCamera)
        {
            dedoCamera = t.fingerId;
        }
    }

    void Segurar(Touch t)
    {
        if (t.fingerId == dedoJoy)
        {
            Vector2 v = (t.position - centroJoy) / raioJoy;
            if (v.magnitude > 1f) v = v.normalized;
            if (v.magnitude < 0.15f) v = Vector2.zero;
            TouchInput.joystick = v;
        }
        else if (t.fingerId == dedoCamera)
        {
            TouchInput.Olhar(t.deltaPosition);
        }
    }

    void Terminar(int id)
    {
        if (dedoJoy == id)
        {
            dedoJoy = -1;
            TouchInput.joystick = Vector2.zero;
        }
        if (dedoCamera == id) dedoCamera = -1;
        foreach (Botao b in botoes)
            if (b.dedo == id) b.dedo = -1;
    }

    void SoltarTudo()
    {
        dedoJoy = -1;
        dedoCamera = -1;
        TouchInput.joystick = Vector2.zero;
        foreach (Botao b in botoes) b.dedo = -1;
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        if (estiloRotulo == null)
        {
            estiloRotulo = new GUIStyle(GUI.skin.label);
            estiloRotulo.alignment = TextAnchor.MiddleCenter;
            estiloRotulo.normal.textColor = Color.white;
        }
        float h = Screen.height;
        estiloRotulo.fontSize = Mathf.RoundToInt(unidade * 0.36f);

        // Joystick: anel externo, anel interno e bolinha
        Desenhar(anel, centroJoy, raioJoy, Color.white, h);
        Desenhar(anel, centroJoy, raioJoy * 0.6f, new Color(1f, 1f, 1f, 0.6f), h);
        Vector2 bolinha = centroJoy + TouchInput.joystick * raioJoy * 0.5f;
        Desenhar(disco, bolinha, raioJoy * 0.4f, new Color(0.75f, 0.75f, 0.75f, 0.9f), h);

        // Botões no estilo Winlator: fundo translúcido, contorno branco, escurece ao apertar
        foreach (Botao b in botoes)
        {
            Vector2 c = b.r.center;
            float raio = b.r.width * 0.5f;
            bool ativo = b.dedo >= 0 || (b.tecla == KeyCode.LeftShift && correr);
            float fundo = ativo ? 0.45f : 0.12f;
            Desenhar(disco, c, raio, new Color(1f, 1f, 1f, fundo), h);
            Desenhar(anel, c, raio, new Color(1f, 1f, 1f, 0.9f), h);
            GUI.color = Color.white;
            GUI.Label(new Rect(b.r.x, h - b.r.yMax, b.r.width, b.r.height), b.rotulo, estiloRotulo);
        }

        // Marca da versão, como na imagem
        GUI.color = Color.white;
        GUIStyle marca = new GUIStyle(estiloRotulo);
        marca.alignment = TextAnchor.LowerLeft;
        marca.fontSize = Mathf.RoundToInt(unidade * 0.18f);
        GUI.Label(new Rect(10f, h - unidade * 0.34f, unidade * 2.4f, unidade * 0.3f), "Unofficial Port.", marca);
    }

    void Desenhar(Texture2D textura, Vector2 centro, float raio, Color cor, float h)
    {
        GUI.color = cor;
        GUI.DrawTexture(new Rect(centro.x - raio, h - centro.y - raio, raio * 2f, raio * 2f), textura);
    }

    // Círculo com borda suavizada. borda = 0 gera disco cheio; borda > 0 gera anel.
    static Texture2D Circulo(int tam, float borda)
    {
        Texture2D t = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[tam * tam];
        float centro = tam * 0.5f;
        float raio = centro - 1f;
        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float dx = x + 0.5f - centro;
                float dy = y + 0.5f - centro;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(raio - d + 0.5f);
                if (borda > 0f) a *= Mathf.Clamp01(d - (raio - borda) + 0.5f);
                px[y * tam + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        t.SetPixels32(px);
        t.Apply();
        return t;
    }
}
