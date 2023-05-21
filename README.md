# 2023Forum
A customisable forum created to demonstrate functionality in ASP.NET Core.
Live version of the website: [https://patforum.azurewebsites.net/](https://patforum.azurewebsites.net/)

## Run the application
Use these instructions to get the project up and running.

### Dependencies
You will need:

* [Visual Studio Code or Visual Studio](https://visualstudio.microsoft.com/vs/) (version 16.3 or later)
* [.NET 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) (It will work with .NET 7 if you change all packages in Forum.csproj from 6.0.0 to 7.0.0)


### Setup
Follow these steps to get your development environment set up:

  1. Clone the repository
  2. From the root directory, restore the required packages by running:
      ```
     dotnet restore
     ```
  3. Then build the solution by running:
     ```
     dotnet build
     ```
  4. Next, navigate to the `\Web` directory, and launch the application:
      ```
     cd src/web
     dotnet run
     ```


  5. Launch [http://localhost:5001](http://localhost:5001) in a web browser to use the application.

## How to use the application
When the website is first opened, a sqlite db file will be created in the Web directory. A user will be created:

Username: root

Password: pass

This will be the "root" user of the application, and the user with the most amount of authority. There can be multiple root users but it is reccomended to only have one.

### Types of user roles
* Root - This is the user created when the application is first run. They have the power to add categories and topics, and delete any post or comment. They can also change the role of any user.
* Admin - These users can also add categories and topics, and delete any post or comment. They can change the role of any user except other Admins and the Root user.
* Moderator - These users can ban/unban other users from categories that they are moderators of. They will also be able to ban posts in those categories (although this functionality has not been implemented yet).
* User - These are regular users. When someone registers, they are assigned this role by default. They can create posts and comments as long as they are not banned from that category.
* Banned - These are users that are banned and will be unable to login to create a post or comment.

### Types of data
* Category - This is essentially the top level of the hierarchy. Categories are displayed on the home page, and contain topics which contain posts which contain comments.
* Topic - Topics are grouped in their relevant category. We can see a topics most recent comment and the time of that comment on the home page. When you navigate to a topic page you will see its list of posts, with the time/user of the most recent comment.
* Post - Any user that is not banned from that category can create a post. These posts contain comments. Posts cannot be deleted unless its by a moderator/admin/root user.
* Comment - The lowest level of the hierarchy. Comments can be edited only by the user that made it. A moderator of that category can delete any comment, as well as root/admin users. The first comment in a post cannot be deleted.

When any type of data is "deleted" its IsActive property is set to false. This acts as though it is deleted but is still present in the database just as a safety measure in case we need to recover the data, or we decide to reactivate the data.

### Admin page
This is where authorised users can go into user management to change the role of a user (where allowed) or change a users priveleges. "Show mod controls" can be toggled if the authorised user does not want the clutter of having add/delete options throughout the website.