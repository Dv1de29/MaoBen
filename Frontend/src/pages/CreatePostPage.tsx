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

        if (!file || !file.type.startsWith('image/')) {
            alert("Please select a valid image file.");
            return;
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
                        <img src={previewUrl} alt="Preview" className="post-preview-img" />
                    ) : (
                        <div className="upload-placeholder">
                            <span>+</span>
                            <p>Click to add photo</p>
                        </div>
                    )}
                    
                    <input 
                        type="file" 
                        accept="image/*" 
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