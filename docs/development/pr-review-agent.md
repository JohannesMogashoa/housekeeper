# HouseKeeper pull-request review agent

HouseKeeper uses a repository-owned GitHub Copilot custom agent and review instructions to make pull-request feedback consistent with the accepted architecture and Foundation backlog.

The AI reviewer supplements—not replaces—the author’s verification and final human approval.

## Repository configuration

| File | Responsibility |
|---|---|
| `.github/agents/housekeeper-reviewer.agent.md` | Defines the read-only `housekeeper-reviewer` custom agent and its finding/output contract |
| `.github/copilot-instructions.md` | Repository-wide architecture, security, persistence, reliability, PWA, testing, and review rules |
| `.github/instructions/dotnet-modules-review.instructions.md` | Applies detailed module, C#, EF Core, PostgreSQL, and worker checks to .NET files |
| `.github/instructions/pwa-review.instructions.md` | Applies browser trust, offline, accessibility, installability, and Playwright checks to PWA files |
| `.github/instructions/infrastructure-review.instructions.md` | Applies identity, Bicep, workflow, migration, deployment, rollback, and cost checks to infrastructure files |
| `.github/pull_request_template.md` | Requires the author to expose issue linkage, risk, tests, migrations, screenshots, and operational evidence |

GitHub loads the custom agent from `.github/agents/*.agent.md` after the profile is merged to the default branch. Copilot code review uses the repository and path-specific instructions when reviewing applicable changes.

## Manual deep review

After this configuration is merged:

1. Open GitHub Copilot Agents for the `JohannesMogashoa/housekeeper` repository.
2. Select `housekeeper-reviewer` from the agent dropdown.
3. Ask it to review the target pull request, including the PR number and linked HK issue.
4. Require the agent to inspect the complete diff, surrounding implementation, tests, migrations, workflows, and architecture documentation.
5. Transfer actionable findings to the PR as review comments or resolve them in the implementation branch.

Suggested prompt:

```text
Review pull request #<number> against its linked HK issue and the HouseKeeper architecture. Report only actionable Blocker, High, Medium, or Low findings with file/line evidence, failure mode, smallest safe correction, and proof required. If no material findings exist, state the residual validation gaps.
```

## Requesting Copilot code review on a PR

For an individual pull request, request GitHub Copilot under the PR's **Reviewers** section. Re-request review after significant corrections or use the repository ruleset option described below to review new pushes automatically.

## One-time automatic-review activation

Automatic reviewer assignment is a repository ruleset setting and is not encoded by the instruction files themselves.

1. Open the repository on GitHub.
2. Go to **Settings → Rules → Rulesets**.
3. Create or edit a branch ruleset targeting the default branch or the desired PR targets.
4. Enable **Automatically request Copilot code review**.
5. Enable **Review new pushes** so corrected commits receive another review.
6. Decide deliberately whether draft pull requests should be reviewed.
7. Keep repository custom instructions enabled under **Settings → Copilot → Code review**.

The reviewer agent profile defines a specialist available for explicit sessions. The automatic review ruleset invokes GitHub Copilot code review, which consumes the same repository/path review instructions.

## Review contract

The reviewer prioritizes:

1. cross-household isolation, authorization, and secret safety;
2. data integrity, migration safety, concurrency, idempotency, and restart recovery;
3. modular-monolith dependency and schema ownership;
4. offline/PWA correctness, accessibility, and browser storage isolation;
5. external-provider failure handling and durable worker semantics;
6. test adequacy, observability, deployment safety, rollback, and documentation drift;
7. maintainability issues with a concrete failure or change-cost impact.

Every finding should include:

- severity: Blocker, High, Medium, or Low;
- changed file and line/range;
- violated invariant or issue requirement;
- realistic failure mode;
- smallest safe correction;
- test or evidence needed to prove the correction.

Questions and non-blocking suggestions remain separate from defects. The reviewer avoids speculative abstractions, unrelated refactors, personal style preferences, and praise-only noise.

## Author workflow

Before requesting review:

1. Link the canonical HK GitHub issue and Notion task in the PR.
2. Complete the pull-request template rather than deleting irrelevant-looking sections without explanation.
3. Run the risk-appropriate tests and link the exact CI run.
4. Include migration/rollout/rollback notes for data changes.
5. Include screenshots or recordings for visible PWA changes.
6. State residual manual validation gaps and follow-up issues.
7. Request Copilot review and perform a human review before merge.

## Limitations

- AI review is non-deterministic and can miss defects or raise false positives.
- The custom agent does not grant itself repository permissions and cannot become an automatic reviewer merely by existing in the branch.
- Automatic review requires Copilot availability and the one-time repository ruleset configuration.
- The agent is configured with read/search tools and should not modify implementation code during review.
- Green CI and a clean AI review do not replace explicit human approval for HouseKeeper changes.

## Primary GitHub documentation

- [Creating custom agents for Copilot cloud agent](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/create-custom-agents)
- [Adding repository custom instructions](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/add-custom-instructions/add-repository-instructions)
- [Using GitHub Copilot code review](https://docs.github.com/en/copilot/how-tos/copilot-on-github/use-copilot-agents/copilot-code-review)
- [Configuring automatic Copilot review](https://docs.github.com/en/copilot/how-tos/copilot-on-github/set-up-copilot/configure-automatic-review)
