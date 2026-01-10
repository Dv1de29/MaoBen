import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import type { PostType } from '../assets/types'; 

import '../styles/EditPost.css';

const EditPostPage = () => {
    const navigate = useNavigate();
    const { post_id } = useParams(); 
    const location = useLocation();

    // --- STATE ---
    const [description, setDescription] = useState("");
    
    // The source of truth for what is displayed (Server URL OR Local Blob)
    const [previewUrl, setPreviewUrl] = useState(""); 
    
    // Explicitly track type so we know if we should render <video> or <img>
    const [mediaType, setMediaType] = useState<"image" | "video">("image");

    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [uploading, setUploading] = useState(false);
    
    // Ref for the hidden input
    const fileInputRef = useRef<HTMLInputElement>(null);

    // Helper: Detect type from Server URL (extensions)
    const getMediaTypeFromUrl = (url: string): "image" | "video" => {
        if (/\.(mp4|webm|ogg|mov)$/i.test(url)) return "video";
        return "image";
    };

    // 1. Fetch Existing Data (Initialize Preview)
    useEffect(() => {
        const fetchPost = async () => {
            if (!post_id) return;

            // A. Optimization: Load from Navigation State
            if (location.state && location.state.postData){
                const data: PostType = location.state.postData;
                initializeForm(data);
                return;
            }

            // B. Fallback: Fetch from API
            try {
                const token = sessionStorage.getItem("userToken");
                const res = await fetch(`/api/Posts/${post_id}`, {
                    method: "GET",
                    headers: {
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json',
                    }
                });

                if (!res.ok) throw new Error(`Fetching error: ${res.status}`);
                const data: PostType = await res.json();
                initializeForm(data);

            } catch(e) {
                console.error("Error fetching post: ", e);
            }
        };

        const initializeForm = (data: PostType) => {
            setDescription(data.description);
            // Initialize preview with the server URL
            setPreviewUrl(data.img_path);
            // Determine initial media type
            setMediaType(getMediaTypeFromUrl(data.img_path));
        };

        fetchPost();
    }, [post_id, location.state]);


    // 2. Handle Text Change
    const handleTextChange = (e: ChangeEvent<HTMLTextAreaElement>) => {
        setDescription(e.target.value);
    };

    // 3. Handle Image Click
    const handleImageClick = () => {
        if (!uploading && fileInputRef.current) {
            fileInputRef.current.click();
        }
    }

    // 4. Handle File Selection (With Preview Update)
    const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (!file) return;

        // Validation
        if (!file.type.startsWith('image/') && !file.type.startsWith('video/')) {
            alert("File must be a video or image");
            return;
        }

        // --- VIDEO HANDLING ---
        if (file.type.startsWith('video/')) {
            const video = document.createElement('video');
            video.preload = 'metadata';
            const tempUrl = URL.createObjectURL(file);
            video.src = tempUrl;

            video.onloadedmetadata = () => {
                URL.revokeObjectURL(tempUrl);
                
                // Duration Check
                if (video.duration > 10) {
                    alert("Video must be 10 seconds or shorter.");
                    if (fileInputRef.current) fileInputRef.current.value = ""; 
                    return; 
                }

                // Success
                setSelectedFile(file);
                const objectUrl = URL.createObjectURL(file);
                setPreviewUrl(objectUrl); // <--- Updates the UI immediately
                setMediaType("video");    // <--- Tells UI to render <video> tag
            };
            return;
        }

        // --- IMAGE HANDLING ---
        setSelectedFile(file);
        const objectUrl = URL.createObjectURL(file);
        setPreviewUrl(objectUrl); // <--- Updates the UI immediately
        setMediaType("image");    // <--- Tells UI to render <img> tag
    }

    // 5. Submit Changes
    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setUploading(true);
        
        try {
            const token = sessionStorage.getItem("userToken");
            
            const formDataPayload = new FormData();
            formDataPayload.append("Description", description);
            
            // Only append file if changed
            if (selectedFile) {
                formDataPayload.append("Image", selectedFile);
            }

            const res = await fetch(`/api/Posts/${post_id}`, {
                method: "PUT",
                headers: {
                    'Authorization': `Bearer ${token}`,
                },
                body: formDataPayload
            });

            if (!res.ok) {
                const contentType = res.headers.get("content-type");
                let errorMessage = "Update failed";
                if (contentType && contentType.includes("application/json")) {
                    const errorData = await res.json();
                    errorMessage = errorData.message || JSON.stringify(errorData);
                } else {
                    errorMessage = await res.text();
                }
                throw new Error(errorMessage);
            }

            navigate(-1); 
        
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        } catch(e: any) {
            console.error("Error updating post: ", e);
            alert(e.message || "Failed to update post.");
        } finally {
            setUploading(false);
        }
    };

    const handleDeletePost = () => {
        const token = sessionStorage.getItem("userToken");
        fetch(`/api/Posts/${post_id}`, {
            method: "DELETE",
            headers: { 'Authorization': `Bearer ${token}` }
        })
        .then(() => navigate(-1));
    }

    // Cleanup Blob URLs on unmount
    useEffect(() => {
        return () => {
            if (previewUrl && previewUrl.startsWith('blob:')) {
                URL.revokeObjectURL(previewUrl);
            }
        }
    }, [previewUrl]);

    return (
        <div className="edit-post-container">
            <form className="edit-post-card" onSubmit={handleSubmit}>
                <h2>Edit Post</h2>

                {/* Media Preview Section */}
                <div className="post-image-wrapper" onClick={handleImageClick}>
                    
                    {/* Render based on Explicit State (not just URL string) */}
                    {mediaType === 'video' ? (
                        <video 
                            src={previewUrl} 
                            controls 
                            muted 
                            loop 
                            playsInline
                            className="post-preview-video" 
                        />
                    ) : (
                        <img 
                            src={previewUrl} 
                            alt="Post Preview" 
                            className={`post-preview-img ${uploading ? 'dimmed' : ''}`} 
                        />
                    )}
                    
                    <div className="overlay-hint">Click to change media</div>
                    
                    <input
                        type="file"
                        accept="image/*,video/*"
                        ref={fileInputRef}
                        onChange={handleFileChange}
                        style={{ display: 'none' }}
                        disabled={uploading}
                    />
                </div>

                {/* Description Section */}
                <div className="input-group">
                    <label>Description</label>
                    <textarea 
                        name="description"
                        rows={6}
                        value={description}
                        onChange={handleTextChange}
                        placeholder="Write a caption..."
                    />
                </div>

                {/* Actions */}
                <div className="button-group">
                    <button 
                        type="button" 
                        className="btn-cancel" 
                        onClick={() => navigate(-1)}
                    >
                        Cancel
                    </button>
                    <button 
                        type="button" 
                        className="btn-delete" 
                        onClick={handleDeletePost}
                    >
                        Delete
                    </button>
                    <button 
                        type="submit" 
                        className="btn-save"
                        disabled={uploading}
                    >
                        {uploading ? "Uploading..." : "Save Changes"}
                    </button>
                </div>
            </form>
        </div>
    );
};

export default EditPostPage;