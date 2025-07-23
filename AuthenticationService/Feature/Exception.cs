using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace AuthenticationService.Feature;

using System;

public static class AppExceptionExtensions
{
    public static IActionResult ToIActionResult(this AppException ex)
    {
        return ex switch
        {
            // Auth
            InvalidEmailOrPasswordException e => new UnauthorizedObjectResult(new { e.Message }),
            EmailNotConfirmedException e => new BadRequestObjectResult(new { e.Message }),
            UserCreationFailedException e => new BadRequestObjectResult(new {e.Message, e.Details}),
            InvalidLinkException e => new BadRequestObjectResult(new { e.Message }),
            UserNotFoundException e => new NotFoundObjectResult(new { e.Message }),
            PasswordResetFailedException e => new BadRequestObjectResult(new {e.Message, e.Details}),
        };
    }
}

// Auth
public class UserCreationFailedException : AppException
{
    public UserCreationFailedException(string message, List<string> details) : base(Error.UserCreationFailed)
    {
        this.Message = message;
        this.Details = details;
    }
    
    public string Message { get; set; }
    public List<string> Details { get; set; }
}

public class UserNotFoundException : AppException
{
    public UserNotFoundException() : base(Error.UserNotFound) { }
}

public class UserAlreadyExistsException : AppException
{
    public UserAlreadyExistsException() : base(Error.UserAlreadyExists) { }
}

public class InvalidEmailException : AppException
{
    public InvalidEmailException() : base(Error.InvalidEmail) { }
}

public class InvalidPasswordException : AppException
{
    public InvalidPasswordException() : base(Error.InvalidPassword) { }
}

public class InvalidEmailOrPasswordException : AppException
{
    public InvalidEmailOrPasswordException() : base(Error.InvalidEmailOrPassword) { }
}

public class InvalidLinkException : AppException
{
    public InvalidLinkException() : base(Error.InvalidLink) { }
}

// Email
public class EmailConfirmationFailedException : AppException
{
    public EmailConfirmationFailedException() : base(Error.EmailConfirmationFailed) { }
}

public class EmailNotConfirmedException : AppException
{
    public EmailNotConfirmedException() : base(Error.EmailNotConfirmed) { }
}

// Password
public class PasswordResetFailedException : AppException
{
    public PasswordResetFailedException(string message, List<string> details) : base(Error.PasswordResetFailed)
    {
        this.Message = message;
        this.Details = details;
    }
    
    public string Message { get; set; }
    public List<string> Details { get; set; }
}