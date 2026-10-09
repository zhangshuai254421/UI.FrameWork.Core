# AGENTS

## Agent skills

### Issue tracker

Issues are tracked as GitHub issues via the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Uses the five default triage labels (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.

### 注释风格

代码注释的第一读者是人（2026-10-09 定。起因：报警模块注释被仓库主人评「写的感觉很合理，但不是给人读的」）。四条：

1. 一句一个意思的普通陈述句；不用破折号接三层、不括号套括号。
2. 直说领域词（搁置标记、写入队列、重试计数），不造隐喻黑话（本子、冲账、对暗号、打标、阴魂）。
3. 保留「为什么」——理由、约束、反面案例是注释的本职，但要用人话讲。
4. 出处引用（摘要 §X / ADR-000Y / issue #NNN）放句尾括号，一句最多一个。
