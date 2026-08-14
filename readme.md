# Тестовое задание для компании «ПАУЭР»

**Исполнитель:** Гонсовский Константин

- HH: https://hh.ru/resume/40b8acccff064127520039ed1f3861676b6837?hhtmFrom=applicant_profile
- Telegram: [@KonstantinGonsovskii](https://t.me/KonstantinGonsovskii)

**Текст задания:** [task.md](task.md)

---

## Выбор стека и архитектуры

| Решение | Выбор |
|--------|--------|
| Framework | .NET 10 |
| UI | Blazor Web App, **Interactive Server** (Blazor Server) |
| Архитектура | Clean Architecture |
| CQRS / use-cases | **MediatR** (`GetWeatherQuery`) |
| HTTP | `Power.Weather.Providers.WeatherDotCom` (`Providers:WeatherDotCom` в appsettings) |
| Конфиг | API key и координаты Москвы в `appsettings` / User Secrets |
| Тесты | xUnit по слоям: Domain/Application/Infrastructure/Providers + Integrity |
| Дизайн | Один экран, спокойный weather-дашборд: текущее → почасовой → 3 дня; loading / error + retry |

### Структура solution

```
src/
  Power.Weather.Domain/
  Power.Weather.Application/
  Power.Weather.Infrastructure/
  Power.Weather.Providers.WeatherDotCom/
  Power.Weather.Web/
tests/
  Power.Weather.Domain.Tests/
  Power.Weather.Application.Tests/
  Power.Weather.Infrastructure.Tests/
  Power.Weather.Providers.WeatherDotCom.Tests/
  Power.Weather.Integrity.Tests/
```

### API-стратегия

Достаточно **`forecast.json?days=3`**: в ответе есть current + hourly + forecast days.  
`current.json` не обязателен отдельно (дублирует данные); при желании можно держать оба вызова — для ТЗ достаточно forecast.

Москва зафиксирована в `appsettings.json` → секция `Location` (`TimeZoneId: Europe/Moscow`).  
Почасовой срез («оставшееся сегодня + всё завтра») считается **на сервере** через domain service `IHourlyForecastSelector`, не в Blazor UI.

### UI (один экран)

1. **Текущая** — температура, ощущается, условие, иконка, ветер/влажность.  
2. **Почасовая** — часы с «сейчас» до конца сегодня + все 24 часа завтра.  
3. **3 дня** — дата, min/max, условие.  
4. **Loading / Error + «Повторить»**.

Запуск UI: `dotnet run --project src/Power.Weather.Web`.
