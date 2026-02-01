-- Update seed recipes with image URLs
-- Run this script to add images to existing seed recipes in the database

USE WorldRecipeDb;
GO

UPDATE Recipes 
SET ImageUrl = 'https://t3.ftcdn.net/jpg/03/01/55/56/360_F_301555615_PuTf9kLR9GqVKDMs8kVdwyUB4yjOjNIz.jpg' 
WHERE RecipeName = 'Brigadeiro';

UPDATE Recipes 
SET ImageUrl = 'https://images.unsplash.com/photo-1612874742237-6526221588e3?w=500' 
WHERE RecipeName = 'Spaghetti Carbonara';

UPDATE Recipes 
SET ImageUrl = 'https://images.unsplash.com/photo-1565557623262-b51c2513a641?w=500' 
WHERE RecipeName = 'Chicken Tikka Masala';

UPDATE Recipes 
SET ImageUrl = 'https://images.unsplash.com/photo-1574071318508-1cdbab80d002?w=500' 
WHERE RecipeName = 'Margherita Pizza';

UPDATE Recipes 
SET ImageUrl = '/WebsiteImages/Protein-Pancakes.jpg' 
WHERE RecipeName = 'Classic Pancakes';

UPDATE Recipes 
SET ImageUrl = 'https://images.unsplash.com/photo-1550304943-4f24f54ddde9?w=500' 
WHERE RecipeName = 'Caesar Salad';

GO

-- Verify the updates
SELECT RecipeID, RecipeName, ImageUrl FROM Recipes WHERE RecipeName IN 
('Brigadeiro', 'Spaghetti Carbonara', 'Chicken Tikka Masala', 'Margherita Pizza', 'Classic Pancakes', 'Caesar Salad');
