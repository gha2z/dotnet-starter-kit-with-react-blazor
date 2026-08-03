Make sure you have updated the "./opencode/addBlazorFrontends/00-Index.md", all plan.md files in the phase folders in 
the "./opencode/addBlazorFrontends" folder based on the phases you've just been working on so any AI Agents and even 
an idiot developer can understand current progress and continue working from where you left off. 

Skip updating the hands-on-phase-x.md files until all phases have completed perfectly.

Write the Last Update date and time and introduce who you are right after the document main title, e.g: 
```00-Index.md
# Blazor WASM + MAUI — Implementation Roadmap
Last Update: <yyyy-MMM-dd hh:mm:ss>, by: <you>.
```
```plan.md
# Phase 2 — Admin Feature Pages
Last Update: <yyyy-MMM-dd hh:mm:ss>, by: <you>.
```
This header goes in every root doc (`00-Index.md`, `00-Setup.md`, `99-Glossary.md`) and in every plan
document inside each phase folder — `plan.md`, or `pre-plan.md` in the `Phase 00-The foundation` folder
(it is the exception: literally named `Phase 00-The foundation`, with a space, and holds `pre-plan.md`
instead of `plan.md`).
Replace `<you>` with your actual identity, e.g. `by: opencode (auto/coding).`, 
or even better `by: opencode (auto/coding, model: mimo-v2.5-free).`.

Write an MD file named "implementation-summary-<yyyy-MM-dd-hh-mm-ss>.md" in the "./opencode/addBlazorFrontends/00_summary/" 
folder containing the summary of implementation steps you have made in a session:
```template
# Implementation Summary <yyyy-MM-dd-hh-mm-ss>

---
**Description:** <The description summary>
**Creator:** <You>
**Duration:** <How long the implementation took place in seconds/minutes/hours, ex: 30s, 1m 15s, 55m, 1h 12m 3s, 3h, and so on>
---

## <Phase N> - <Phase Name>: <Sub Feature N> - <Sub Feature Name>
---
- Step 1: <Description summary of the step 1>
- Step 2: <Description summary of the step 2>
...
- Step N: <Description summary of the step N>

## <Phase N> - <Phase Name>: <Sub Feature N> - <Sub Feature Name>
---
- Step 1: <Description summary of the step 1>
- Step 2: <Description summary of the step 2>
...
- Step N: <Description summary of the step N>
```

Check on "./opencode/addBlazorFrontends/00-Index.md", the latest implementation-summary-*.md files in 
"./opencode/addBlazorFrontends/00_summary/", and plan.md files in 
"./opencode/addBlazorFrontends/Phase-<phase-number>-<phase-name>" (except `Phase 00-The foundation`, see above) to plan your next moves.

Refer to the original repository (https://github.com/fullstackhero/dotnet-starter-kit),
website (https://fullstackhero.net/) to execute the plans, review your progress and keep building, testing, fixing, 
and optimizing until all the plans completed and no more gaps between React 19 front-ends and Blazor Wasm front-ends 
and .NET MAUI front-ends.

Do not write/modify anything in any folders other than blazor wasm and .net maui blazor hybrid front-ends project folders 
and sub-folders ("./clients/FSH.Hybrid", "./clients/admin-blazor", "./clients/BlazorShared", "./clients/dashboard-blazor") 
and "./opencode/addBlazorFrontends".

Once you completed all phases, keep testing them to find the bugs and issues and fix and enhance them accordingly to ensure 
these projects are production-grade ready. Feel free to add/modify phases, and plans when necessary. 
You can modify the existing @AGENTS.md, @README.md, @README-template.md along with Agent Skill.md and rules files, 
and add the new ones related to the current BLAZOR WASM and .NET MAUI Hybrid front-ends projects when necessary.

Test the apps yourself and fix any issues relevant to the current development progress after you finish the implementations.

---
run FSH.Starter.AppHost and wait until everything is wired up

fsh-starter-admin (react)
http://localhost:5173
Credential: 
tenant:root
email:admin@root.com
password:Password123!

fsh-starter-dashboard (react)
http://localhost:5174
Credential: 
tenant:acme
email:admin@acme.com
password:Password123!

fsh-starter-admin-blazor
FSH.Admin.Wasm.csproj
http://localhost:5175
Credential: 
tenant:root
email:admin@root.com
password:Password123!

fsh-starter-dashboard-blazor
FSH.Dashboard.Wasm.csproj
http://localhost:5176
Credential: 
tenant:acme
email:admin@acme.com
password:Password123!

---