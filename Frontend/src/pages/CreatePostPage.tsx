import { useState, useRef, type ChangeEvent, type FormEvent, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import '../styles/CreatePostPage.css'; // We will create this CSS file below

const CreatePostPage = () => {
    const navigate = useNavigate();

    // STATE
    const [description, setDescription] = useState("");
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [previewUrl, setPreviewUrl] = useState<string | null>(null);
    const [uploading, setUploading] = useState(false);

    // REFS
    const fileInputRef = useRef<HTMLInputElement>(null);

    const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];

        if (!file || (!file.type.startsWith('image/') && !file.type.startsWith('video/'))) {
            alert("Please select a valid image or video file.");
            return;
        }

        // 2. If it's a VIDEO, check duration
        if (file.type.startsWith('video/')) {
            const video = document.createElement('video');
            video.preload = 'metadata';
            
            // Create a temporary URL to load the video metadata
            const tempUrl = URL.createObjectURL(file);
            video.src = tempUrl;

            video.onloadedmetadata = () => {
                // Clean up the temp URL immediately
                URL.revokeObjectURL(tempUrl);

                // Check Duration (e.g., 10 seconds)
                if (video.duration > 10) {
                    alert("Video must be 10 seconds or shorter.");
                    if (fileInputRef.current) fileInputRef.current.value = ""; 
                    return; 
                }

                // Duration is OK -> Proceed
                setSelectedFile(file);
                setPreviewUrl(URL.createObjectURL(file)); // Create final preview URL
            };

            return; // Stop here, the 'onloadedmetadata' callback handles the rest
        }

        setSelectedFile(file);
        
        const objectUrl = URL.createObjectURL(file);
        setPreviewUrl(objectUrl);
    };

    const triggerFileSelect = () => {
        if (fileInputRef.current && !uploading) {
            fileInputRef.current.click();
        }
    };

    // 3. Handle Submit
    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();

        if (!selectedFile) {
            alert("Please select an image for your post.");
            return;
        }

        setUploading(true);

        try {
            const token = sessionStorage.getItem("userToken");
            
            const formData = new FormData();
            formData.append('description', description);
            formData.append('image', selectedFile);

            const res = await fetch("/api/Posts/create_post", {
                method: "POST",
                headers: {
                    'Authorization': `Bearer ${token}`
                },
                body: formData
            });

            if (!res.ok) {
                throw new Error(`Upload failed: ${res.status} ${res.statusText}`);
            }

            navigate("/");

        } catch (error) {
            console.error("Error creating post:", error);
            alert("Failed to create post.");
        } finally {
            setUploading(false);
            if (previewUrl) URL.revokeObjectURL(previewUrl);
        }
    };

    useEffect(() => {
        return () => {
            if (previewUrl) {
                URL.revokeObjectURL(previewUrl);
            }
        };
    }, [previewUrl]);

    return (
        <div className="create-post-container">
            <form className="create-post-card" onSubmit={handleSubmit}>
                <h2>Create New Post</h2>

                {/* Image Upload Section */}
                <div 
                    className={`image-upload-area ${!previewUrl ? 'empty' : ''}`} 
                    onClick={triggerFileSelect}
                >
                    {previewUrl ? (
                        // Check if the selected file is a video
                        selectedFile?.type.startsWith('video/') ? (
                            <video 
                                src={previewUrl} 
                                controls 
                                className="preview-media" // Ensure you have CSS for this
                            />
                        ) : (
                            <img 
                                src={previewUrl} 
                                alt="Preview" 
                                className="preview-media" 
                            />
                        )
                    ) : (
                        <div className="placeholder">Click to upload image or video</div>
                    )}
                    
                    <input 
                        type="file" 
                        accept="image/*,video/*"
                        ref={fileInputRef} 
                        onChange={handleFileChange} 
                        style={{ display: 'none' }} 
                    />
                </div>

                {/* Description Section */}
                <div className="input-group">
                    <label>Caption</label>
                    <textarea 
                        rows={4}
                        placeholder="Write a caption..."
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        disabled={uploading}
                    />
                </div>

                {/* Buttons */}
                <div className="button-group">
                    <button 
                        type="button" 
                        className="btn-cancel"
                        onClick={() => navigate(-1)}
                        disabled={uploading}
                    >
                        Cancel
                    </button>
                    <button 
                        type="submit" 
                        className="btn-save"
                        disabled={uploading || !selectedFile}
                    >
                        {uploading ? "Posting..." : "Share"}
                    </button>
                </div>
            </form>
        </div>
    );
};

export default CreatePostPage;