# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 語言設定 Language Setting

**重要：請永遠使用繁體中文與使用者溝通。**
**IMPORTANT: Always communicate with users in Traditional Chinese.**

## Project Overview

This repository contains the foundational design documentation for an ADHD Productivity Management System. It serves as the comprehensive blueprint for developing a productivity system specifically designed for individuals with ADHD, addressing their unique neurological challenges.

## Core Documentation Architecture

The repository contains three critical design documents that form the foundation of the system:

### 1. ADHD 生產力系統建構指南_.md
The comprehensive productivity system construction guide that outlines:
- Neurological foundations of ADHD challenges (executive dysfunction, time blindness, dopamine economy, emotional dysregulation)
- Four foundational pillars: thorough externalization, comprehensive visualization, friction-free engineering, and resilient design
- System architecture cycle: Capture → Prioritize → Plan → Initiate → Feedback → Repeat
- Detailed implementation strategies for each component
- Tool selection and integration guidelines

### 2. 系統分析與設計書.md
The complete system analysis and design specification including:
- Business and functional requirements analysis
- ASP.NET Core-based system architecture design (4-layer: Presentation, Application, Domain, Infrastructure)
- Database design with PostgreSQL, Redis caching strategy
- CQRS pattern implementation with MediatR
- SignalR real-time communication architecture
- Security framework (JWT, OAuth 2.0, RBAC)
- Performance optimization and monitoring strategies
- Deployment architecture using Docker containerization

### 3. 視覺設計指南.md
The comprehensive visual design guide covering:
- ADHD-centered design philosophy focusing on cognitive load reduction
- Complete design system (colors, typography, spacing, components)
- Accessibility implementation (WCAG 2.1 AA compliance)
- ADHD-specific UX patterns and interaction design
- Dark mode and responsive design specifications
- Animation and transition guidelines for ADHD users

## Key Design Principles

When working with this codebase, understand these core principles that drive all decisions:

### ADHD-Centered Design
- **External Structure**: Provide external executive function support for planning and organization
- **Visual Time Management**: Make abstract time concepts tangible through visual indicators
- **Low Friction Interactions**: Minimize cognitive overhead in all user interactions
- **Emotional Safety**: Design for psychological safety and self-compassion

### Technical Architecture Principles
- **Clean Architecture**: 4-layer separation with dependency inversion
- **CQRS Pattern**: Command/Query separation for optimal performance
- **Event-Driven Design**: Loose coupling through domain and integration events
- **API-First**: RESTful design with comprehensive OpenAPI documentation

### System Integration Strategy
- **Multi-Platform Sync**: Seamless experience across web, mobile, and desktop
- **External Integrations**: Email (IMAP/SMTP), calendar sync, notification services
- **Real-Time Features**: SignalR for live updates, shared focus sessions, timer synchronization
- **Progressive Web App**: Offline capability and native-like experience

## Development Context

This repository represents the design phase output from a collaborative analysis involving:
- Product Manager agent analyzing user requirements and system objectives
- Senior Backend Engineer agent designing ASP.NET Core architecture
- Senior Frontend Engineer agent planning React/TypeScript implementation

The documentation provides complete specifications for implementing a production-ready ADHD productivity management system with enterprise-level architecture and ADHD-specific user experience optimization.

## Database Configuration

The system uses a single Docker-based deployment with internal PostgreSQL database:

### Container Configuration
```bash
# 啟動系統 (唯一方式)
docker-compose up -d

# 容器內資料庫資訊：
Database Name: adhd_productivity
Username: adhd_user
Password: adhd_secure_pass_2024
Host: adhd-postgres (容器內部)
```

### Database Management Commands

```bash
# 啟動系統
docker-compose up -d

# 連接到資料庫
docker exec -it adhd-postgres psql -U adhd_user -d adhd_productivity

# 查看資料庫狀態
docker exec -it adhd-postgres psql -U adhd_user -d adhd_productivity -c "\dt"

# 停止系統
docker-compose down
```

### Test Accounts

```
Demo Account: demo@adhd.dev / demo123
Test Account: test@adhd.dev / test123
Admin Account: admin@adhd.dev / admin123
```

## Working with Documentation

When referencing these documents:
- Treat them as authoritative specifications for system behavior
- Use the neurological insights to inform UX/UI decisions
- Follow the technical architecture patterns consistently
- Reference specific sections when implementing features (documents include detailed line numbers)
- Consider ADHD user needs in all technical decisions

The documents are written in Traditional Chinese but contain extensive technical terminology and system specifications that are universally applicable to software development.

## Memory Notes

- 遭遇問題不應該採用補丁或簡化方式，深度思考找尋核心問題才開始規劃分析解決方案並執行