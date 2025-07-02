# Contributing to ADHD Productivity System

Thank you for your interest in contributing to the ADHD Productivity System! This project aims to provide tools specifically designed for individuals with ADHD, and we welcome contributions that further this mission.

## 🎯 Our Mission

We're building a productivity system that understands and accommodates the unique cognitive patterns of ADHD. Every contribution should align with our core principles:

- **ADHD-Centered Design**: Features should reduce cognitive load and support executive function
- **Neurodiversity Inclusion**: Design with neurodivergent users as the primary audience
- **Evidence-Based**: Features should be grounded in ADHD research and user feedback
- **Accessible**: Code and interfaces should be accessible to all users
- **Sustainable**: Consider the long-term maintainability and user impact

## 🚀 Getting Started

### Prerequisites

- **Node.js** 20+ and npm
- **.NET 8.0** SDK
- **Docker** and Docker Compose
- **Git** with proper configuration
- **PostgreSQL** 16+ (or use Docker)
- **Redis** 7+ (or use Docker)

### Development Environment Setup

1. **Fork and Clone**
   ```bash
   git clone https://github.com/your-username/ADHD-productivity-system.git
   cd ADHD-productivity-system
   ```

2. **Install Dependencies**
   ```bash
   # Frontend dependencies
   cd frontend
   npm install
   
   # Backend Node.js dependencies
   cd ../backend
   npm install
   
   # Backend .NET dependencies
   dotnet restore
   ```

3. **Environment Configuration**
   ```bash
   # Copy environment files
   cp .env.example .env
   cp frontend/.env.example frontend/.env
   cp backend/.env.example backend/.env
   
   # Edit configuration files with your settings
   ```

4. **Database Setup**
   ```bash
   # Start services with Docker
   docker-compose up -d postgres redis
   
   # Run migrations
   npm run migrate
   
   # Seed test data
   npm run seed
   ```

5. **Start Development Servers**
   ```bash
   # Terminal 1: Backend
   cd backend && npm run dev
   
   # Terminal 2: Frontend
   cd frontend && npm run dev
   
   # Terminal 3: .NET API (if contributing to .NET components)
   cd backend && dotnet run --project src/AdhdProductivitySystem.Api
   ```

6. **Verify Setup**
   - Frontend: http://localhost:3000
   - Backend API: http://localhost:5000
   - API Documentation: http://localhost:5000/swagger

## 📋 Types of Contributions

### 🐛 Bug Reports

When reporting bugs, please include:

- **ADHD Impact**: How does this bug affect users with ADHD specifically?
- **Reproduction Steps**: Clear, step-by-step instructions
- **Expected vs Actual**: What should happen vs what actually happens
- **Environment**: Browser, OS, version information
- **Screenshots/Videos**: Visual evidence when applicable
- **Urgency Level**: How critical is this for daily ADHD management?

**Bug Report Template:**
```markdown
## Bug Description
Brief description of the bug and its impact on ADHD users.

## ADHD User Impact
Describe how this affects cognitive load, executive function, or task management.

## Steps to Reproduce
1. Step one
2. Step two
3. Step three

## Expected Behavior
What should happen?

## Actual Behavior
What actually happens?

## Environment
- OS: [e.g., Windows 11, macOS 13]
- Browser: [e.g., Chrome 119, Firefox 120]
- Version: [e.g., v1.0.0]

## Additional Context
Any other context, screenshots, or related issues.
```

### ✨ Feature Requests

For new features, consider:

- **ADHD Benefit**: How will this help users with ADHD?
- **Research Basis**: Is this supported by ADHD research or user studies?
- **Cognitive Load**: Will this increase or decrease mental effort?
- **Implementation Complexity**: Consider development and maintenance overhead
- **Alternative Solutions**: Are there simpler ways to achieve the goal?

**Feature Request Template:**
```markdown
## Feature Summary
Brief description of the proposed feature.

## ADHD User Benefit
Explain how this feature will specifically help users with ADHD.

## Research/Evidence
Link to research or user feedback supporting this feature.

## User Story
As a [user type], I want [goal] so that [benefit].

## Acceptance Criteria
- [ ] Criterion 1
- [ ] Criterion 2
- [ ] Criterion 3

## Design Considerations
- Cognitive load impact
- Accessibility requirements
- Integration with existing features

## Implementation Notes
Technical considerations or suggestions.
```

### 🔧 Code Contributions

#### Code Style and Standards

**Frontend (React/TypeScript)**
```typescript
// ✅ Good: Clear component with ADHD considerations
interface QuickCaptureProps {
  onCapture: (text: string) => void;
  maxLength?: number;
  adhdFriendly?: boolean; // Enable ADHD-specific features
}

const QuickCapture: React.FC<QuickCaptureProps> = ({ 
  onCapture, 
  maxLength = 500,
  adhdFriendly = true 
}) => {
  // Clear, descriptive variable names
  const [inputText, setInputText] = useState('');
  const [isProcessing, setIsProcessing] = useState(false);
  
  // ADHD-friendly: Show character count for awareness
  const remainingChars = maxLength - inputText.length;
  
  return (
    <div className="quick-capture" data-testid="quick-capture">
      <textarea
        value={inputText}
        onChange={(e) => setInputText(e.target.value)}
        placeholder="What's on your mind? (quick capture)"
        maxLength={maxLength}
        aria-label="Quick thought capture"
        aria-describedby="char-count"
      />
      {adhdFriendly && (
        <div id="char-count" className="char-count">
          {remainingChars} characters remaining
        </div>
      )}
    </div>
  );
};
```

**Backend (.NET/C#)**
```csharp
// ✅ Good: Clear service with proper error handling
public class TaskService : ITaskService
{
    private readonly ILogger<TaskService> _logger;
    private readonly ITaskRepository _repository;

    public async Task<TaskDto> CreateTaskAsync(CreateTaskCommand command)
    {
        try 
        {
            // Validate ADHD-specific requirements
            ValidateAdhdRequirements(command);
            
            var task = new TaskItem
            {
                Title = command.Title,
                Priority = command.Priority,
                EstimatedDuration = command.EstimatedDuration,
                EnergyLevel = command.EnergyLevel ?? EnergyLevel.Medium
            };

            await _repository.AddAsync(task);
            
            _logger.LogInformation("Task created successfully for user {UserId}", 
                command.UserId);
                
            return _mapper.Map<TaskDto>(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task for user {UserId}", 
                command.UserId);
            throw;
        }
    }
    
    private void ValidateAdhdRequirements(CreateTaskCommand command)
    {
        // ADHD-specific validation
        if (command.EstimatedDuration > 60)
        {
            throw new ArgumentException(
                "Tasks longer than 60 minutes should be broken down for ADHD users");
        }
    }
}
```

#### Testing Requirements

**Test Coverage Standards**
- **Minimum 70% code coverage** for all new code
- **Unit tests** for all business logic
- **Integration tests** for API endpoints
- **Component tests** for React components
- **Accessibility tests** for UI components

**Example Test Structure**
```typescript
// Frontend component test
describe('QuickCapture', () => {
  it('shows character count for ADHD users', () => {
    render(<QuickCapture onCapture={jest.fn()} adhdFriendly={true} />);
    
    const input = screen.getByLabelText('Quick thought capture');
    fireEvent.change(input, { target: { value: 'Test input' } });
    
    expect(screen.getByText('490 characters remaining')).toBeInTheDocument();
  });
  
  it('prevents input beyond max length', () => {
    const longText = 'a'.repeat(501);
    render(<QuickCapture onCapture={jest.fn()} maxLength={500} />);
    
    const input = screen.getByLabelText('Quick thought capture');
    fireEvent.change(input, { target: { value: longText } });
    
    expect(input.value).toHaveLength(500);
  });
});
```

```csharp
// Backend service test
[Test]
public async Task CreateTask_WithLongDuration_ThrowsValidationException()
{
    // Arrange
    var command = new CreateTaskCommand
    {
        Title = "Long task",
        EstimatedDuration = 120 // Too long for ADHD users
    };

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentException>(
        () => _taskService.CreateTaskAsync(command));
    
    Assert.That(exception.Message, 
        Does.Contain("should be broken down for ADHD users"));
}
```

#### Commit Message Guidelines

We follow [Conventional Commits](https://www.conventionalcommits.org/) with ADHD-specific additions:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

**Types:**
- `feat`: New feature (especially ADHD-beneficial features)
- `fix`: Bug fix (especially ADHD-impacting bugs)
- `adhd`: ADHD-specific improvements or features
- `a11y`: Accessibility improvements
- `ux`: User experience improvements
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `ci`: CI/CD changes

**Examples:**
```
feat(tasks): add energy-based task filtering for ADHD users

Implements energy level filtering to help users match tasks to their
current cognitive capacity. Includes low/medium/high energy categories
with visual indicators.

Closes #123
```

```
fix(timer): resolve interruption handling in focus sessions

Fixed issue where interrupting a focus session would lose progress
data, causing frustration for ADHD users who need flexible session
management.

Fixes #456
ADHD-Impact: High (affects core focus session functionality)
```

## 🧪 Testing Guidelines

### Running Tests

```bash
# Frontend tests
cd frontend
npm test                    # Run all tests
npm run test:watch         # Watch mode
npm run test:coverage      # With coverage report

# Backend Node.js tests
cd backend
npm test                   # Run all tests
npm run test:integration   # Integration tests only

# Backend .NET tests
cd backend
dotnet test               # Run all .NET tests
dotnet test --collect:"XPlat Code Coverage"  # With coverage
```

### Test Categories

1. **Unit Tests**: Test individual functions/components in isolation
2. **Integration Tests**: Test API endpoints and database interactions
3. **Component Tests**: Test React components with user interactions
4. **Accessibility Tests**: Ensure ADHD-friendly design compliance
5. **Performance Tests**: Verify response times meet ADHD requirements

### ADHD-Specific Testing

- **Cognitive Load Tests**: Verify interfaces don't overwhelm users
- **Attention Span Tests**: Ensure quick feedback and minimal delays
- **Error Recovery Tests**: Test gentle error handling and recovery
- **Interruption Tests**: Verify graceful handling of task interruptions

## 🎨 Design Guidelines

### ADHD-Friendly Design Principles

1. **Minimize Cognitive Load**
   - Use clear, simple language
   - Avoid overwhelming interfaces
   - Provide visual hierarchy
   - Limit choices when possible

2. **Support Executive Function**
   - Break complex tasks into steps
   - Provide clear next actions
   - Include progress indicators
   - Offer undo/redo functionality

3. **Manage Attention**
   - Use consistent navigation
   - Minimize distractions
   - Provide focus modes
   - Include attention restoration breaks

4. **Emotional Safety**
   - Use encouraging language
   - Avoid shame-inducing patterns
   - Provide choice and control
   - Include self-compassion reminders

### UI/UX Standards

- **Colors**: Follow ADHD-friendly color palette
- **Typography**: Use dyslexia-friendly fonts
- **Spacing**: Adequate white space to reduce overwhelm
- **Animation**: Purposeful, non-distracting animations
- **Feedback**: Immediate, clear feedback for all actions

## 🔒 Security Requirements

### Security Standards

1. **Input Validation**: All user inputs must be validated and sanitized
2. **Authentication**: Proper JWT token handling and rotation
3. **Authorization**: Role-based access control for all endpoints
4. **Data Protection**: Encrypt sensitive ADHD-related data
5. **Error Handling**: Never expose sensitive information in errors

### ADHD Data Sensitivity

ADHD-related data requires special handling:
- **Medical Information**: Treat ADHD type/symptoms as medical data
- **Behavioral Patterns**: Focus/attention data is privacy-sensitive
- **Progress Data**: Productivity metrics may reveal personal struggles
- **Communication**: All data handling must be transparent to users

## 📖 Documentation Standards

### Code Documentation

```typescript
/**
 * QuickCapture component for ADHD users
 * 
 * Provides a low-friction way to capture thoughts and tasks quickly,
 * reducing the cognitive load of context switching and helping users
 * externalize their mental processing.
 * 
 * @param onCapture - Callback when user captures a thought
 * @param maxLength - Maximum character limit (default: 500)
 * @param adhdFriendly - Enable ADHD-specific features (default: true)
 * 
 * @example
 * ```tsx
 * <QuickCapture 
 *   onCapture={(text) => handleNewThought(text)}
 *   adhdFriendly={true}
 * />
 * ```
 */
```

### API Documentation

- Update OpenAPI specifications for any API changes
- Include ADHD-specific considerations in endpoint descriptions
- Provide realistic examples that demonstrate ADHD use cases
- Document rate limits and error handling

### User Documentation

- Write from the perspective of ADHD users
- Use clear, actionable language
- Include screenshots and videos when helpful
- Provide troubleshooting for common ADHD-related issues

## 🤝 Community Guidelines

### Communication

- **Be Patient**: Contributors may have ADHD or other neurodivergent conditions
- **Be Clear**: Use specific, actionable language
- **Be Supportive**: Celebrate contributions and learning
- **Be Inclusive**: Welcome all backgrounds and experience levels

### Code Review Process

1. **Automated Checks**: All PRs must pass CI/CD pipeline
2. **ADHD Impact Review**: Consider how changes affect ADHD users
3. **Security Review**: Verify security best practices
4. **Performance Review**: Ensure acceptable response times
5. **Accessibility Review**: Check for accessibility compliance

### Review Checklist

- [ ] Code follows style guidelines
- [ ] Tests are included and passing
- [ ] Documentation is updated
- [ ] ADHD user impact is considered
- [ ] Security requirements are met
- [ ] Performance impact is acceptable
- [ ] Accessibility standards are maintained


### Mental Health Support

Working on ADHD-related tools can be emotionally challenging. If you need support:

- **Crisis Resources**: [International crisis helplines](https://findahelpline.com)
- **ADHD Support**: [ADHD support organizations](https://chadd.org)
- **Developer Wellness**: [Resources for developer mental health](https://osmihelp.org)

## 📜 License

By contributing to this project, you agree that your contributions will be licensed under the [MIT License](./LICENSE).

## 🙏 Recognition

We believe in recognizing all types of contributions:

- **Code Contributors**: Listed in our contributors page
- **ADHD Consultants**: Recognized for lived experience insights
- **Community Moderators**: Acknowledged for creating safe spaces
- **Documentation Writers**: Credited for making knowledge accessible
- **Testers**: Recognized for ensuring quality and usability

### Hall of Fame

We maintain a Hall of Fame for significant contributors:
- Outstanding ADHD advocacy in feature design
- Exceptional accessibility improvements
- Significant performance optimizations
- Community building and support

---

**Thank you for contributing to a more ADHD-friendly world! 🧠💙**

---

*Last Updated: December 22, 2024*  
*Contributing Guide Version: 1.0.0*
