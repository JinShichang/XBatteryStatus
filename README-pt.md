# 🔋 XBatteryStatus

> Monitoree o nível de bateria dos seus dispositivos Bluetooth na bandeja do sistema do Windows, com **aviso emergente estilo conquista do Xbox** quando a bateria está baixa (Conquista desbloqueada: o seu controle está morrendo 🤣)

🌐 **Idiomas:** [English](README.md) | [简体中文](README-zh-CN.md) | [Русский](README-ru.md) | [Español](README-es.md) | [Português](README-pt.md) | [Deutsch](README-de.md) | [日本語](README-ja.md) | [Français](README-fr.md) | [Polski](README-pl.md) | [한국어](README-ko.md) | [العربية](README-ar.md)


## 📸 Demonstração

![Demo 1: animação do aviso](Icons/1.gif)

![Demo 2](Icons/2.jpg)

![Demo 3](Icons/3.jpg)

![Demo 4](Icons/4.jpg)

![Demo 5](Icons/5.jpg)

---

## ✨ Recursos

- 🎮 **Monitoramento de vários dispositivos**: descobre automaticamente todos os dispositivos Bluetooth pareados com serviço de bateria (controles do Xbox, fones Bluetooth, teclados e mouses Bluetooth, controles de outras marcas, etc.), sem duplicatas por **endereço Bluetooth**, então dispositivos com o mesmo nome nunca se confundem
- 🟢 **Aviso estilo conquista do Xbox**: um círculo aparece no centro inferior → expande-se em um cartão → um brilho o percorre → recolhe automaticamente. Uma réplica fiel da animação de conquistas do Xbox Game Bar no Windows 11
- 🎚️ **Três níveis de aviso personalizáveis**: cada dispositivo pode ter seus próprios 3 valores (padrão 35% / 30% / 25%). O aviso dispara uma vez ao cruzar um limite, com detecção de quedas bruscas (ex.: 51% → 49% também dispara)
- ✏️ **Aviso totalmente personalizável**: texto de título/subtítulo (com os espaços reservados `{battery}` e `{device}`), fontes independentes por linha, escala geral (100%–200%), **posicionamento interativo** (setas para ajustar, Enter para confirmar)
- 🔔 **Sons de aviso**: vários sons integrados, suporte a arquivos `.wav` personalizados (basta colocá-los na pasta `sound` ao lado do exe), mudo por padrão
- 🌍 **Interface multilíngue**: chinês simplificado, inglês, russo, espanhol, português, alemão, japonês, francês, polonês, coreano, árabe; muda automaticamente para chinês em sistemas chineses
- 🚀 **Iniciar com o Windows**: ativado por padrão, alternância com um clique
- 📄 **Alternância de registro**: ative registros detalhados quando necessário, gravados na pasta do exe (limite de 10 MB, truncamento automático)
- 📁 **Portátil**: todos os arquivos (configuração, registro, sons) ficam dentro da pasta do exe — não suja o sistema

## ⚙️ Uso

O aplicativo vive na bandeja do sistema. Clique com o botão direito no ícone:

| Item do menu | Descrição |
|---|---|
| 🌗 Theme | Tema do ícone: Automático / Claro / Escuro |
| 🌐 Language | Idioma da interface (Auto segue o sistema) |
| 🎮 Devices | Gerenciamento de dispositivos: avisos, limites, nomes, intervalo de leitura, registro |
| 🎨 Popup Settings | Personalização do aviso: texto, fontes, tamanho, posição |
| 🚀 Iniciar com o Windows | Executar no logon (ativado por padrão) |
| 🔔 Som | Escolher o som do aviso (mudo por padrão) |
| ❌ Exit | Sair do aplicativo |

## 🎨 Configurações do aviso

- **Título / Subtítulo**: texto personalizado; deixe vazio para usar o texto localizado padrão. O subtítulo aceita espaços reservados:
  - `{battery}` → o nível real da bateria (ex.: `35`)
  - `{device}` → o nome exibido do dispositivo
- **Fontes**: a linha do título e a do subtítulo têm cada uma a sua **própria fonte independente** (tamanho, peso)
- **Tamanho**: escala geral de 100% a 200%, tudo aumenta proporcionalmente
- **Posição**: clique em "Set Position..." e o aviso permanece visível; **as setas movem 1 px por toque, 10 px ao segurar**, `Enter` confirma, `Esc` cancela; "Reset Position" volta ao centro inferior a qualquer momento
- **Teste**: mostra um aviso de teste imediatamente; "Test (5s)" mostra 5 segundos depois, útil para verificar cenários em tela cheia em um jogo

## 🎮 Dispositivos

- Lista automaticamente todos os dispositivos Bluetooth pareados com serviço de bateria (sem duplicatas por endereço)
- Por dispositivo você pode configurar:
  - ✅ Se os avisos de bateria fraca estão ativados (controles ativados por padrão)
  - 🔢 3 valores de bateria para avisar (1–100%)
  - ✏️ Um nome exibido personalizado (vazio = nome do dispositivo; usado pelo espaço reservado `{device}`)
- **Intervalo de leitura**: com que frequência a bateria é lida, em segundos (padrão 15 s)
- **Ativar registro**: quando marcado, registros detalhados são gravados

## 🌍 Idiomas

Suporta 11 idiomas: chinês simplificado, inglês, russo, espanhol, português, alemão, japonês, francês, polonês, coreano, árabe.

- **Auto** (padrão): usa chinês quando o idioma do sistema é chinês simplificado; caso contrário, inglês; os demais idiomas são selecionados manualmente
- A troca manual está disponível no menu da bandeja

## 🔔 Som

- Menu da bandeja "Som" → "Mudo" ou escolha um arquivo de áudio (os itens do menu são listados pelo nome do arquivo)
- Sons personalizados: coloque arquivos `.wav` na **pasta `sound` ao lado do exe**; o menu é atualizado automaticamente, sem reiniciar
- Apenas `.wav` é suportado

## 📁 Arquivos e diretórios

O aplicativo é portátil por design — tudo é gerado no **diretório do exe**:

```
📁 diretório do exe/
├── 📄 XBatteryStatus.exe      ← programa principal
├── 📄 log.txt                 ← registro (gravado com o registro ativado, limite 10 MB)
├── 📁 sound/                  ← pasta de sons (somente leitura, coloque seus wav aqui)
└── 📁 config/                 ← toda a configuração
    ├── 📄 settings.json       ← configurações do aplicativo (idioma/aviso/som/inicialização etc.)
    └── 📄 devices.json        ← configuração de avisos por dispositivo
```

## ⚠️ Observações

- Jogos em **tela cheia exclusiva** (Exclusive Fullscreen) não podem exibir o aviso — é uma limitação do Windows que se aplica a todas as ferramentas de terceiros; mude seus jogos para o modo **sem bordas** (Borderless Windowed)
- Para sons personalizados, apenas arquivos `.wav` são suportados
- Os avisos seguem a política "**uma vez por cruzamento de limite**": só avisa novamente quando a bateria é recarregada acima do limite e cai abaixo de novo; um dispositivo que reconecta já abaixo do limite também avisa uma vez

## 🛠️ Compilação

Requer o **.NET 8 SDK** (Windows).

```bash
dotnet build XBatteryStatus/XBatteryStatus.csproj -c Release
```

A saída é gravada em `XBatteryStatus/bin/Release/net8.0-windows10.0.19041.0/`:

- Os sons integrados são copiados automaticamente para a pasta `sound/` da saída
- `config/` e `log.txt` são gerados em tempo de execução ao lado do exe; nada precisa ser criado manualmente

## ❓ Perguntas frequentes

- P: Por que este projeto foi criado?

- R: As ferramentas existentes avisam por meio de notificações do Windows, mas enquanto você joga o Windows bloqueia por padrão notificações que não são de prioridade máxima, e você precisa definir a prioridade máxima manualmente — é chato. Mesmo depois de configurar, com o tempo o Windows pergunta se deseja desativar as notificações do app de bateria do controle, porque você nunca abriu as notificações; a Microsoft acha que não é importante e recomenda desativá-las. Por isso contornei o canal nativo de notificações e implementei os avisos de outra forma.

- P: Por que o adaptador não mostra o nível exato da bateria?

- R: O XInput oferece apenas 4 níveis (Empty vazio / Low baixo / Medium médio / Full cheio); não há porcentagem precisa como no GATT Bluetooth. É uma limitação do próprio protocolo XInput; não há nada que eu possa fazer.

Suporta vários dispositivos

## 🙏 Referências e créditos

> https://github.com/NiyaShy/XB1ControllerBatteryIndicator

> https://github.com/tommaier123/XBatteryStatus

> https://github.com/SteamAchievementNotifier/SteamAchievementNotifier

> https://github.com/gopi470/Nox

> https://github.com/SpartanX1/bluetooth_classic_battery_windows

> https://github.com/o0Zz/PeripheralBatteryMonitor

## 🍋 Apoio

Gostou? Pague-me um copo de limonada ~

![QR code de doação Alipay](Icons/zfb.jpg)


## 📄 Licença

- Este projeto é publicado sob a licença **GPL-3.0**, veja [LICENSE](LICENSE).
