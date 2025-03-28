# Image Reconstruction Using HTM and KNN

## Abstract
Efficient image storage and reconstruction are critical in the era of increasing digital content. Traditional image storage methods require significant memory, making retrieval costly and inefficient. This paper presents a novel image reconstruction framework that compresses images into Sparse Distributed Representations (SDRs) using a biologically inspired approach based on Hierarchical Temporal Memory (HTM). The system employs K-Nearest Neighbors (KNN) and HTM-based classifiers to reconstruct images with high accuracy while significantly reducing storage requirements. The framework is trained on 10,000 images and tested on 2,000 images, achieving robust reconstruction with minimal data loss. Leveraging the NeoCortex API for SDR generation, this methodology optimizes storage efficiency without compromising image fidelity. The proposed approach has the potential to revolutionize cloud storage, AI-powered image enhancement, and digital content management by offering an ultra-efficient yet accurate image compression and retrieval system.

## Solution Architecture
The workflow of the project follows these key steps:

1. **Binarization**: Convert grayscale images into binary images.
2. **Vectorization**: Transform binarized images into a 1D vector format.
3. **Spatial Pooling**: Extract Sparse Distributed Representations (SDRs) using a spatial pooler.
4. **Training**: Train separate HTM and KNN classifiers on the SDR representations.
5. **Prediction**: Use the trained classifiers to reconstruct images from test data.
6. **Combination of Classifiers**: Merge results from HTM and KNN using Gaussian weighting and neighboring cell interactions.
7. **Evaluation**: Compare reconstructed images with original images using cosine similarity.
8. **Post-processing**: Apply a median filter to refine binary images.
9. Conversion of Binary images to png images.

### Architecture Diagram
![Solution Architecture](https://github.com/Eyad-Elsheita/Code_Avengers/tree/development/Image_Reconstruction_Classifier/Documentation)  
Please refer to images Test Data Flowchart.png & Train Data Flowchart.png in project repository.

## Setup Instructions
Follow these steps to reproduce the project:

### Prerequisites
- **Programming Language**: C# (.NET)
- **Required Libraries**:
  - ClosedXML
  - HtmImageEncoder
  - LearningApi
  - NeoCortexApi
  - ImageBinarizer
  - SkiaSharp
  - SixLabors.ImageSharp
- **Development Environment**:
  - Visual Studio / JetBrains Rider
  - .NET SDK Installed

### Environment Variables
Set the following environment variables to ensure proper execution:

```sh
export Similarity_Statistics="path/to/similarity/statistics"
export Test_Image_Binary="path/to/test_images_binary"
export Test_Image_Loader="path/to/test_image_loader"
export Test_Image_Spatial="path/to/test_image_spatial"
export Test_Images="path/to/test_images"
export Test_Images_Reconstructed_Binary_Combined="path/to/reconstructed_test_binary_images_combined"
export Test_Images_Reconstructed_Binary_HTM="path/to/reconstructed_test_binary_images_htm"
export Test_Images_Reconstructed_Binary_KNN="path/to/reconstructed_test_binary_images_knn"
export Test_Images_Reconstructed_Combined="path/to/reconstructed_test_images_combined"
export Test_Images_Reconstructed_Vector_Combined="path/to/reconstructed_test_vector_images_combined"
export Test_Images_Reconstructed_Vector_HTM="path/to/reconstructed_test_vector_images_htm"
export Test_Images_Reconstructed_Vector_KNN="path/to/reconstructed_test_vector_images_knn"
export Training_Image_Binary="path/to/training_image_binary"
export Training_Image_Loader="path/to/training_image_loader"
export Training_Image_Sample="path/to/training_image_sample"
export Training_Image_Spatial="path/to/training_image_spatial"
```
*(On Windows, set environment variables using `System Properties > Advanced > Environment Variables`.)*

### Running the Project in Visual Studio
1. Open the project in **Visual Studio**.
2. Ensure all required **environment variables** are set.
3. Install any missing **NuGet packages**.
4. Click **Run** to execute the pipeline.
5. The program will automatically:
   - Binarize images
   - Vectorize images
   - Process them through the spatial pooler
   - Train HTM and KNN classifiers
   - Predict test images
   - Generate and save reconstructed images
   - Generate and save similarity statistics in excel sheets.

## Unit Testing & Documentation
## Link to  Documentation [Link](https://github.com/Eyad-Elsheita/Code_Avengers/tree/development/Image_Reconstruction_Classifier/Documentation/README.md)
## Link to  UnitTesting [Link](https://github.com/Eyad-Elsheita/Code_Avengers/tree/development/Image_Reconstruction_Classifier/UnitTests)

## Results
| Classifier  | Vector Images (%) | Binary Images (%) |
|-------------|------------------|------------------|
| KNN         | 83.39            | 87.00           |
| HTM         | 85.90            | 87.28           |
| Combined    | 86.22            | 88.12           |

## Contributors
- Mohamed Adel M Abughrara
- Taha Balaban
- Eyad Elsheita

