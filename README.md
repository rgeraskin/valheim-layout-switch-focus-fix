# LayoutSwitchFocusFix

Typing no longer stops when you switch the keyboard layout. **EN** | [RU ниже](#русская-версия)

## English

In vanilla Valheim, switching the keyboard layout while typing (Alt+Shift, Win+Space, Ctrl+Space, Caps Lock or any other hotkey) drops the text field you're typing in: the next keys go to the game instead, and you have to click the field again. It happens everywhere:

- chat and the F5 console
- map pin names
- sign and portal texts
- the build menu (hammer) search
- the crafting search of mods like Workbenches+
- any other mod's text field

This mod fixes the cause, so the field simply keeps focus and you keep typing in the new layout.

**Using [Valheim Community Patch](https://thunderstore.io/c/valheim/p/MidnightMods/ValheimCommunityPatch/)?** You don't need this mod: it was merged into Valheim Community Patch in version 0.42.0 and is on by default there (`Fix Layout Switch Focus`).

### What's going on

A layout switch makes Unity's Input System report a keyboard *configuration change*. Valheim treats every such change as a switch between keyboard and gamepad, and the build menu answers it by clearing the UI selection, which takes keyboard focus away from whatever field you're in. The mod lets the build menu do everything it did except that, and only while you're typing with the keyboard. Switching to a gamepad behaves as before.

### Notes

- Client-side only: nothing on the server, safe in multiplayer.
- No per-frame work: one patch on a game method that runs only when the layout changes.
- Tested on Windows and macOS. On Linux it does the same if the game sees layout switches there; otherwise there's nothing to fix.
- Works alongside ConsoleFix and other input mods.
- Config `BepInEx/config/rgeraskin.LayoutSwitchFocusFix.cfg`: `Enabled`, and `Debug.Log` to log layout changes and every focus loss with its cause (helps with bug reports).

### Bugs and ideas

Found a bug or have an idea? Tell me on the [GitHub issues page](https://github.com/rgeraskin/valheim-layout-switch-focus-fix/issues) or in the chat for my mods, on [Discord](https://discord.gg/hxpuTQF2Bb) or [Telegram](https://t.me/+NwReoOS9x7hhYjgy).

### Made with AI

This mod — code, images and this text — was made entirely with AI, and tested in the game.

## Русская версия

В ванильном Valheim смена раскладки во время ввода (Alt+Shift, Win+Space, Ctrl+Space, Caps Lock или любая другая комбинация) сбрасывает фокус с поля ввода: следующие нажатия уходят в игру, приходится заново кликать в поле. Это происходит везде:

- чат и консоль F5
- названия меток на карте
- текст табличек и порталов
- поиск в меню строительства (молот)
- поиск рецептов в модах вроде Workbenches+
- поля ввода любых других модов

Мод устраняет причину: поле просто не теряет фокус, и вы продолжаете печатать в новой раскладке.

**Используете [Valheim Community Patch](https://thunderstore.io/c/valheim/p/MidnightMods/ValheimCommunityPatch/)?** Этот мод вам не нужен: он вошёл в Valheim Community Patch в версии 0.42.0 и включён там по умолчанию (`Fix Layout Switch Focus`).

### В чём дело

При смене раскладки Input System в Unity сообщает об *изменении конфигурации* клавиатуры. Valheim принимает любое такое изменение за переключение между клавиатурой и геймпадом, и меню строительства в ответ сбрасывает выделение в интерфейсе, а вместе с ним и фокус с поля ввода. Мод оставляет меню строительства всё, кроме этого сброса, и только пока вы печатаете с клавиатуры. Переключение на геймпад работает как раньше.

### Примечания

- Только на клиенте: на сервер ставить не нужно, безопасен в мультиплеере.
- Ничего не делает каждый кадр: один патч на метод игры, который вызывается только при смене раскладки.
- Проверен на Windows и macOS. На Linux работает так же, если игра там замечает смену раскладки; если нет, то и чинить нечего.
- Совместим с ConsoleFix и другими модами ввода.
- Конфиг `BepInEx/config/rgeraskin.LayoutSwitchFocusFix.cfg`: `Enabled` и `Debug.Log`, чтобы писать в лог смены раскладки и каждую потерю фокуса с её причиной (пригодится для баг-репортов).

### Баги и идеи

Нашли баг или есть идея? Пишите на [странице GitHub issues](https://github.com/rgeraskin/valheim-layout-switch-focus-fix/issues) или в чат моих модов, в [Discord](https://discord.gg/hxpuTQF2Bb) или [Telegram](https://t.me/+NwReoOS9x7hhYjgy).

### Сделано с ИИ

Этот мод — код, картинки и этот текст — целиком сделан с помощью ИИ и проверен в игре.
