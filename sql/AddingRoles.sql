USE WorldRecipeDb;

SELECT * FROM AspNetUsers;
SELECT * FROM AspNetRoles;
SELECT * FROM AspNetUserRoles;

-- Inserting the ROLE
INSERT INTO AspNetRoles (Id, [Name], NormalizedName, ConcurrencyStamp)
   VALUES (NEWID(), 'Admin', 'ADMIN', NEWID());

   INSERT INTO AspNetRoles (Id, [Name], NormalizedName, ConcurrencyStamp)
   VALUES (NEWID(), 'User', 'USER', NEWID());

     INSERT INTO AspNetRoles (Id, [Name], NormalizedName, ConcurrencyStamp)
   VALUES (NEWID(), 'Moderator', 'MODERATOR', NEWID());


   -- GET USER ID
   SELECT * FROM AspNetUsers;

   --ROLE ID
   SELECT Id FROM AspNetRoles WHERE [Name] = 'Admin';

   -- ASSIGN ROLE TO THE USER

   INSERT INTO AspNetUserRoles (UserId, RoleId)
   VALUES ('9e511037-a18b-4f3a-a450-4c1a3cd45af8', '17ce8470-2110-4e76-b54a-c4723c24aa91');

