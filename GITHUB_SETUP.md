# GitHub Repository Setup Guide

This guide will help you create a new GitHub repository and push your RLCSWrapper code to it.

## Step 1: Create a New Repository on GitHub

1. Go to [GitHub](https://github.com) and sign in
2. Click the **"+"** icon in the top right corner
3. Select **"New repository"**
4. Fill in the repository details:
   - **Repository name**: `RLCSWrapper` (or your preferred name)
   - **Description**: "C# .NET 10 wrapper for RL (Robotic Library) trajectory planning"
   - **Visibility**: Choose Public or Private
   - **DO NOT** initialize with README, .gitignore, or license (we already have these)
5. Click **"Create repository"**

## Step 2: Connect Your Local Repository to GitHub

After creating the repository, GitHub will show you commands. Use these commands in your terminal:

### Option A: Using HTTPS (Recommended for beginners)

```powershell
cd C:\Tools\RoboticLibrary\GitHub\RLCSWrapper

# Add the remote repository (replace YOUR_USERNAME with your GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/RLCSWrapper.git

# Rename the default branch to 'main' (if GitHub uses 'main' instead of 'master')
git branch -M master

# Push your code to GitHub
git push -u origin master
```

### Option B: Using SSH (If you have SSH keys set up)

```powershell
cd C:\Tools\RoboticLibrary\GitHub\RLCSWrapper

# Add the remote repository (replace YOUR_USERNAME with your GitHub username)
git remote add origin git@github.com:YOUR_USERNAME/RLCSWrapper.git

# Rename the default branch to 'main' (if GitHub uses 'main' instead of 'master')
git branch -M main

# Push your code to GitHub
git push -u origin main
```

## Step 3: Verify the Push

1. Go to your GitHub repository page
2. You should see all your files, including:
   - README.md
   - .gitignore
   - Source code files
   - Documentation

## Troubleshooting

### Authentication Issues (HTTPS)

If you get authentication errors when pushing:

- GitHub no longer accepts passwords for HTTPS. You need to use a Personal Access Token:
  1. Go to GitHub Settings → Developer settings → Personal access tokens → Tokens (classic)
  2. Generate a new token with `repo` permissions
  3. Use the token as your password when prompted

### Branch Name Mismatch

If GitHub expects `main` but your local branch is `master`:

```powershell
git branch -M main
git push -u origin main
```

### Remote Already Exists

If you get "remote origin already exists":

```powershell
# Remove the existing remote
git remote remove origin

# Add it again with the correct URL
git remote add origin https://github.com/YOUR_USERNAME/RLCSWrapper.git
```

## Next Steps

After successfully pushing:

1. **Add a License**: Consider adding a LICENSE file to your repository
2. **Set up GitHub Actions**: Consider adding CI/CD workflows for automated builds
3. **Add Topics/Tags**: Add relevant topics to your repository (e.g., `csharp`, `robotics`, `trajectory-planning`, `dotnet`)
4. **Create Releases**: Tag important versions and create releases

## Future Updates

To push future changes:

```powershell
cd C:\Tools\RoboticLibrary\GitHub\RLCSWrapper

# Stage your changes
git add .

# Commit with a descriptive message
git commit -m "Description of your changes"

# Push to GitHub
git push
```
