# ADHD Productivity System API Documentation

## Overview

The ADHD Productivity System API is designed specifically for individuals with ADHD, providing task management, focus sessions, and productivity analytics optimized for executive function challenges.

## 🎯 ADHD-Centered Design Philosophy

Our API design prioritizes:
- **Cognitive Load Reduction**: Simplified request/response structures
- **Executive Function Support**: Built-in task decomposition and prioritization
- **Time Management**: Advanced scheduling and reminder systems
- **Dopamine Regulation**: Reward systems and progress tracking
- **Emotional Safety**: Gentle error handling and confirmation patterns

## 📚 Documentation Resources

### API Specifications
- **[OpenAPI Specification](./openapi.yml)** - Complete API definition in OpenAPI 3.0 format
- **[API Contracts](./api-contracts.md)** - Quick reference for endpoints and examples
- **[Authentication Guide](./authentication.md)** - Detailed authentication implementation
- **[WebSocket Events](./websocket-events.md)** - Real-time communication documentation

### Integration Guides
- **[Getting Started](./getting-started.md)** - Quick start guide for developers
- **[SDK Documentation](./sdks/)** - Official SDKs and libraries
- **[Postman Collection](./postman/)** - Ready-to-use API collection
- **[Code Examples](./examples/)** - Implementation examples in multiple languages

## 🚀 Quick Start

### 1. Authentication

All API requests require authentication via JWT tokens:

```bash
curl -X POST https://api.adhd-productivity.dev/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "your-password"
  }'
```

### 2. Create Your First Task

```bash
curl -X POST https://api.adhd-productivity.dev/v1/tasks \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Complete project proposal",
    "priority": "high",
    "estimatedDuration": 60,
    "energyLevel": "medium"
  }'
```

### 3. Start a Focus Session

```bash
curl -X POST https://api.adhd-productivity.dev/v1/focus-sessions \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "taskId": "your-task-id",
    "duration": 25,
    "backgroundSound": "white_noise"
  }'
```

## 🔧 API Features

### Task Management
- **Smart Decomposition**: Automatic task breaking for complex projects
- **Energy-Based Filtering**: Match tasks to current energy levels
- **Context Switch Warnings**: Minimize cognitive overhead
- **Priority Matrix**: ADHD-friendly prioritization system

### Focus Sessions
- **Customizable Durations**: Adapt to individual attention spans
- **Interruption Handling**: Gentle pause/resume functionality
- **Background Sounds**: Focus-enhancing audio options
- **Progress Tracking**: Real-time session monitoring

### Analytics & Insights
- **Attention Patterns**: Identify optimal focus times
- **Productivity Trends**: Track improvement over time
- **ADHD-Specific Metrics**: Specialized analytics for ADHD users
- **Dopamine Tracking**: Monitor reward system effectiveness

## 🔒 Security & Privacy

### Security Features
- **JWT Authentication**: Secure token-based authentication
- **Rate Limiting**: Protection against abuse
- **Input Validation**: Comprehensive data sanitization
- **HTTPS Encryption**: All communications encrypted

### Privacy Considerations
- **Data Minimization**: Only collect necessary information
- **ADHD-Sensitive Data**: Enhanced protection for mental health information
- **User Control**: Granular privacy settings
- **GDPR Compliance**: Full compliance with data protection regulations

## 📊 Rate Limits

| Endpoint Type | Rate Limit | Window |
|---------------|------------|---------|
| Authentication | 5 requests | 1 minute |
| Standard APIs | 100 requests | 1 minute |
| File Uploads | 10 requests | 1 minute |
| Analytics | 20 requests | 1 minute |

## 🌍 Environments

| Environment | Base URL | Purpose |
|-------------|----------|---------|
| Production | `https://api.adhd-productivity.dev/v1` | Live system |
| Staging | `https://staging-api.adhd-productivity.dev/v1` | Testing |
| Development | `http://localhost:5000/v1` | Local development |

## 📱 SDKs and Libraries

### Official SDKs
- **JavaScript/TypeScript**: [@adhd-productivity/api-client](https://www.npmjs.com/package/@adhd-productivity/api-client)
- **Python**: [adhd-productivity-python](https://pypi.org/project/adhd-productivity/)
- **C#/.NET**: [AdhdProductivity.ApiClient](https://www.nuget.org/packages/AdhdProductivity.ApiClient/)

### Community SDKs
- **React Hooks**: [react-adhd-productivity](https://www.npmjs.com/package/react-adhd-productivity)
- **Vue.js Plugin**: [vue-adhd-productivity](https://www.npmjs.com/package/vue-adhd-productivity)

## 🔄 WebSocket Events

Real-time updates are available via WebSocket connections:

```javascript
import io from 'socket.io-client';

const socket = io('wss://api.adhd-productivity.dev', {
  auth: { token: 'YOUR_JWT_TOKEN' }
});

// Focus session events
socket.on('focus-session:started', (session) => {
  console.log('Session started:', session);
});

socket.on('focus-session:completed', (session) => {
  console.log('Session completed:', session);
});

// Task events
socket.on('task:created', (task) => {
  console.log('New task:', task);
});
```

## 📈 Monitoring & Status

- **API Status**: [https://status.adhd-productivity.dev](https://status.adhd-productivity.dev)
- **Performance Metrics**: Available in developer dashboard
- **Incident Reports**: Automatic notifications for service disruptions

## 🆘 Support & Help

### Documentation Support
- **GitHub Issues**: [API Documentation Issues](https://github.com/adhd-productivity/api-docs/issues)
- **Discussion Forum**: [Developer Community](https://community.adhd-productivity.dev)
- **Stack Overflow**: Use tag `adhd-productivity-api`

### Technical Support
- **Email**: api-support@adhd-productivity.dev
- **Response Time**: 24 hours for general inquiries, 4 hours for critical issues
- **Developer Portal**: [https://developers.adhd-productivity.dev](https://developers.adhd-productivity.dev)

## 🔄 Changelog & Versioning

### Current Version: v1.0.0

We follow [Semantic Versioning](https://semver.org/):
- **Major versions**: Breaking changes
- **Minor versions**: New features, backward compatible
- **Patch versions**: Bug fixes and improvements

### Recent Updates
- **v1.0.0** (2024-12-22): Initial public release
  - Complete task management API
  - Focus session tracking
  - Basic analytics endpoints
  - WebSocket real-time updates

### Deprecation Policy
- **30 days notice** for minor breaking changes
- **90 days notice** for major version changes
- **Migration guides** provided for all breaking changes

## 🧪 Testing

### API Testing Tools
- **Postman Collection**: [Download here](./postman/ADHD-Productivity-API.postman_collection.json)
- **Insomnia Workspace**: [Import workspace](./insomnia/ADHD-Productivity-API.json)
- **OpenAPI Testing**: Compatible with any OpenAPI 3.0 testing tool

### Test Environment
- **Base URL**: `https://test-api.adhd-productivity.dev/v1`
- **Test Users**: Available in developer documentation
- **Reset Data**: Test data resets every 24 hours

## 🤝 Contributing

We welcome contributions to improve our API documentation:

1. **Fork** the documentation repository
2. **Create** a feature branch
3. **Make** your improvements
4. **Submit** a pull request

### Documentation Standards
- Use clear, concise language
- Include ADHD-specific considerations
- Provide practical examples
- Test all code samples

## 📄 License

This API documentation is licensed under [MIT License](./LICENSE).

---

**Need Help?** 
- 📧 Email: api-support@adhd-productivity.dev
- 💬 Chat: Available in developer portal
- 📞 Phone: +1-XXX-XXX-XXXX (business hours)

**Last Updated**: December 22, 2024  
**Documentation Version**: 1.0.0