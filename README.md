# C# Coursework — 231-321

Бекмуратов Дилмурат Бекмуратович, группа 231-321

Два проекта для зачёта по курсу C# (семестр 6).

## Проекты

- [ClockTrainer](ClockTrainer/) — тренажёр определения времени. Два упражнения («выставь стрелки на циферблате» и «посчитай, сколько времени прошло»), каждое доступно в режиме обучения и проверки знаний, три уровня сложности. Консольный клиент + WinForms, общая логика — в переиспользуемой библиотеке `libClockCore`.
- *(второй проект — свободная тема, пока не начат)*

## ClockTrainer — сборка и запуск

**Visual Studio 2026:** открыть `ClockTrainer/prjClockTrainer.slnx`, выбрать стартовым проектом `wfaClockTrainer` (или `cnsClockTrainer`), F5.

**VS Code:** из корня репозитория —
```bash
dotnet run --project ClockTrainer/wfaClockTrainer/wfaClockTrainer.csproj
dotnet run --project ClockTrainer/cnsClockTrainer/cnsClockTrainer.csproj
```
Либо просто F5 — в `.vscode/launch.json` уже настроены обе конфигурации (нужно расширение C# / C# Dev Kit).

**Параметры командной строки** (общие для обоих клиентов, `--help` печатает то же самое):
```
--mode=learn|test               режим: обучение (показывает ответ) / проверка знаний
--exercise=set|elapsed          упражнение: выставить стрелки / посчитать разницу времени
--difficulty=easy|medium|hard   сложность (шаг генерации времени: 30 / 5 / 1 минута)
--rounds=N                      количество раундов (только консоль, по умолчанию 5)
-h, --help                      справка
```

**Управление в WinForms:** мышью — перетащить стрелку часов; `Enter` — проверить ответ, `N` — новое упражнение, `F1` — справка, `Ctrl+,` — настройки (тема). Настройки и последний выбранный режим/сложность хранятся в `%AppData%\ClockTrainer\settings.json` — общий формат для консоли и WinForms.

**Демонстрация без UI:** [ClockTrainer/_notebooks/clock-trainer-demo.ipynb](ClockTrainer/_notebooks/clock-trainer-demo.ipynb) — использование `libClockCore` напрямую из кода плюс примеры CLI-запуска (нужна собранная `libClockCore.dll`: `dotnet build ClockTrainer/libClockCore/libClockCore.csproj`).
