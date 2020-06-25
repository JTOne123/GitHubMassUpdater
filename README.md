
Github repo mass update console applications
==============================

![dot net core cool](https://github.com/JTOne123/GitHubMassUpdater/blob/master/dotnetcore.png?raw=true)

# Motivation
To apply cool features globally

# Reason
No reason, just have a few free hours on a weekend

# Steps
1. Search for git repositories on the Nuget portal
2. Check if the repo needs to be fixed
3. Apply the patch and submit the pull request

# How to run
Find all GitHub repos of nugget packages
```
GitHubMassUpdater.NugetSearch.exe -o c:\temp\nugetsearch_0_10_1000\ -f 0 -l 10 -s 1000
```

Check the trunk branch if it contains a lack of correction
```
GitHubMassUpdater.GitHubSearch.exe -i c:\temp\nugetsearch_0_10_1000\ -o c:\temp\githubsearch_0_10_1000\ -a [gitHubOauth](https://help.github.com/en/github/authenticating-to-github/creating-a-personal-access-token-for-the-command-line) -b master
```

Apply fix
```
```

# What's next?
Feel free to create a PR and make suggestions that we will update next

<a href="https://www.buymeacoffee.com/pauldatsiuk" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/purple_img.png" alt="Buy Me A Coffee" style="height: auto !important;width: auto !important;" ></a>