# Project Description

This is an undignified hack of RobThree's MongoRepository to make available the new async methods in the Official MongoDB C# Driver. It borrows the MongoRepository pattern, and lazily strips out all the parts I didn't need to port to the 2.0 driver (such as the MongoRepositoryManager).

All credit to [RobThree](https://github.com/RobThree/MongoRepository) for the original idea and effort. I have merely hacked his work to pieces.
