# Development Guidelines

## Git Commit Standard for All Agents

### After Completing Any Phase:
1. **Review all files** you modified and ensure they're saved
2. **Stage all changes** using: `git add .`
3. **Commit with this exact message format:**
```
git commit -m "Phase X: [Description] - [Agent Name]

- [Key accomplishment 1]
- [Key accomplishment 2]
- [Key accomplishment 3]
- [Any important notes]
- [Database/UI/Architecture changes]

🤖 Generated with [Claude Code](https://claude.ai/code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

4. **Verify the commit** using: `git log --oneline -1`

### Example:
```
git commit -m "Phase 2D: Database Integration & Sorting Station - Claude Code

- Resolved EF Core SQLite compatibility issue
- Replaced mock data with live database queries
- Fixed character encoding for navigation arrows
- Implemented basic Sorting Station functionality
- Database file (shopfloor.db) created with seeded data
- All existing UI and navigation preserved

🤖 Generated with [Claude Code](https://claude.ai/code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

### Multi-Agent Coordination:
- Always read `AGENT_HANDOFF_LOG.md` before starting
- Update `PROJECT_STATUS.md` with your progress
- Document any blockers or issues immediately
- Create clear handoff notes for the next agent

## File Ownership (Multi-Agent):
- Core files (Program.cs, .csproj): Single owner only
- Station pages: Each agent owns different stations
- Documentation: Separate sections per agent

## Development Process:
1. Read existing documentation and handoff logs
2. Use TodoWrite tool to plan your phase implementation
3. Implement changes systematically
4. Test functionality thoroughly
5. Commit using the standard format above
6. Update handoff documentation for next agent

This creates our self-reinforcing development process where each agent follows the same standards and leaves clear trails for collaboration.