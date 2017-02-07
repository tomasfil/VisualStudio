﻿using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using GitHub.Authentication;
using GitHub.Services;
using GitHub.ViewModels;
using NSubstitute;
using Octokit;
using ReactiveUI;
using Xunit;

namespace UnitTests.GitHub.App.ViewModels
{
    public class TwoFactorDialogViewModelTests
    {
        public class TheShowMethod
        {
            [Fact]
            public void ClearsIsBusy()
            {
                var target = CreateTarget();
                var exception = new TwoFactorChallengeFailedException();

                target.IsBusy = true;
                target.Show(new TwoFactorRequiredUserError(exception));

                Assert.False(target.IsBusy);
            }

            [Fact]
            public void InvalidAuthenticationCodeIsSetWhenRetryFailed()
            {
                var target = CreateTarget();
                var exception = new TwoFactorChallengeFailedException();

                target.Show(new TwoFactorRequiredUserError(exception));

                Assert.True(target.InvalidAuthenticationCode);
            }

            [Fact]
            public async Task OkCommandCompletesAndReturnsNullWithNoAuthorizationCode()
            {
                var target = CreateTarget();
                var exception = new TwoFactorChallengeFailedException();
                var userError = new TwoFactorRequiredUserError(exception);
                var task = target.Show(userError).ToTask();

                target.OkCommand.Execute(null);
                var result = await task;

                // This isn't correct but it doesn't matter as the dialog will be closed.
                Assert.True(target.IsBusy); 

                Assert.Null(result);
            }

            [Fact]
            public async Task OkCommandCompletesAndReturnsAuthorizationCode()
            {
                var target = CreateTarget();
                var exception = new TwoFactorChallengeFailedException();
                var userError = new TwoFactorRequiredUserError(exception);
                var task = target.Show(userError).ToTask();

                target.AuthenticationCode = "123456";
                target.OkCommand.Execute(null);

                var result = await task;
                Assert.True(target.IsBusy);
                Assert.Equal("123456", result.AuthenticationCode);
            }

            [Fact]
            public async Task CancelCommandCompletesAndReturnsNull()
            {
                var target = CreateTarget();
                var exception = new TwoFactorChallengeFailedException();
                var userError = new TwoFactorRequiredUserError(exception);
                var task = target.Show(userError).ToTask();

                target.AuthenticationCode = "123456";
                target.CancelCommand.Execute(null);
                var result = await task;

                Assert.False(target.IsBusy);
                Assert.Null(result);
            }

            [Fact]
            public async Task ResendCodeCommandCompletesAndReturnsRequestResendCode()
            {
                var target = CreateTarget();
                var exception = new TwoFactorChallengeFailedException();
                var userError = new TwoFactorRequiredUserError(exception);
                var task = target.Show(userError).ToTask();

                target.AuthenticationCode = "123456";
                target.ResendCodeCommand.Execute(null);
                var result = await task;

                Assert.False(target.IsBusy);
                Assert.Equal(TwoFactorChallengeResult.RequestResendCode, result);
            }
           
            [Fact]
            public async Task ShowErrorMessageIsClearedWhenAuthenticationCodeSent()
            {
                var target = CreateTarget();
                var exception = new TwoFactorChallengeFailedException();
                var userError = new TwoFactorRequiredUserError(exception);
                var task = target.Show(userError).ToTask();

                Assert.True(target.ShowErrorMessage);
                target.ResendCodeCommand.Execute(null);

                var result = await task;
                Assert.False(target.ShowErrorMessage);
            }

            TwoFactorDialogViewModel CreateTarget()
            {
                var browser = Substitute.For<IVisualStudioBrowser>();
                var twoFactorChallengeHandler = Substitute.For<IDelegatingTwoFactorChallengeHandler>();
                return new TwoFactorDialogViewModel(browser, twoFactorChallengeHandler);
            }
        }
    }
}
