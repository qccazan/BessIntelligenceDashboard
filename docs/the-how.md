# BESS Intelligence Dashboard — How We Built It

## How we developed it

We started from a **single requirements document** that described every feature the product needed — data specifications, screen layouts, and acceptance criteria for each feature.

From that document, we used **spec-driven development with AI agents**. We created five specialized Copilot agents. First, the **story parser** split the requirements document into structured, individual feature specs. Then the **planner** broke each feature down into implementation tasks, and the **backend developer**, **frontend developer**, and **tester agents** generated the code. The tester agent writes automated end-to-end tests for every acceptance criterion.

We set up a **CI/CD pipeline** with GitHub Actions that automatically builds the application, runs all tests, and deploys — so there's no manual work between writing code and having it live.

## The technology

On the backend, we have a **.NET 10 API** connected to a **SQL database**. Since this is a proof of concept, the system **simulates integration with external APIs** — market prices, weather data, battery telemetry — by seeding the database with realistic synthetic data covering a full year.

The intelligence engine uses **ML.NET** — Microsoft's machine learning library — with two models: one that **predicts solar energy production** from weather conditions, and one that **predicts battery degradation costs** from operating patterns. These feed into a **7-step dispatch engine** that finds the most profitable time windows to charge and discharge, checks which batteries are healthy enough to participate, and produces a recommendation with a confidence score.

The frontend is a **React** application that visualizes the fleet, battery states, historical replay, and market forecasts.

Everything is **monitored with OpenTelemetry** sending data to Azure Application Insights, and the whole solution is **deployed to Azure**.
