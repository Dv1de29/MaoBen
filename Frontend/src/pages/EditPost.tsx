import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import type { PostType } from '../assets/types'; 

import '../styles/EditPost.css';

const EditPostPage = () => {
    const navigate = useNavigate();
    const { post_id } = useParams(); // Get the ID from the route (e.g., /post/edit/:post_id)

    const location = useLocation();

    // State for the form data
    const [formData, setFormData] = useState({
        description: "",
        image_path: "",
    });

    const [uploading, setUploading] = useState(false);
    
    // To revert if needed or compare changes
    const originalImageRef = useRef(""); 
    const fileInputRef = useRef<HTMLInputElement>(null);

    // 1. Fetch the existing Post data
    useEffect(() => {
        const fetchPost = async () => {
            if (!post_id) return;

            if ( location.state && location.state.postData ){
                console.log("FROM STATE LOCATION");
                const data: PostType = location.state.postData;

                originalImageRef.current = data.img_path
                setFormData({
                    description: data.description,
                    image_path: data.img_path
                });
                return;
            }

            try {
                const token = sessionStorage.getItem("userToken");
                // Assuming GET /api/Posts/{id} exists
                const res = await fetch(`/api/Posts/${post_id}`, {
                    method: "GET",
                    headers: {
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json',
                    }
                });

                if (!res.ok) {
                    throw new Error(`Fetching error: ${res.status}, ${res.statusText}`);
                }

                // Assuming the API returns a standard Post object
                const data: PostType = await res.json();

                // Store original values
                originalImageRef.current = data.img_path;

                setFormData({
                    description: data.description,
                    image_path: data.img_path
                });

            } catch(e) {
                console.error("Error fetching post: ", e);
                // Optional: navigate back if post doesn't exist
                // navigate(-1);
            }
        }

        fetchPost();
    }, [post_id, location.state]);


    // 2. Handle Text Change
    const handleChange = (e: ChangeEvent<HTMLTextAreaElement>) => {
        setFormData(prev => ({
            ...prev,
            description: e.target.value
        }));
    };

    // 3. Handle Image Click (Trigger File Input)
    const handleImageClick = () => {
        if (!uploading && fileInputRef.current) {
            fileInputRef.current.click();
        }
    }

    // 4. Handle File Selection & Upload
    const handleFileChange = async (e: ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (!file || !file.type.startsWith('image/')) return;

        // Create local preview immediately
        const previewUrl = URL.createObjectURL(file);
        setFormData(prev => ({ ...prev, image_path: previewUrl }));

        setUploading(true);

        const formDataPayload = new FormData();
        formDataPayload.append('image_path', file); 

        try {
            const token = sessionStorage.getItem("userToken");
            // Assuming an endpoint exists to upload post images, or reuse a generic one
            const res = await fetch("/api/Posts/upload_image", { 
                method: "POST",
                headers: {
                    'Authorization': `Bearer ${token}`
                },
                body: formDataPayload
            });

            if (!res.ok) throw new Error(`Error response: ${res.status}`);

            const result: { filePath: string } = await res.json();

            // Update state with the server-side path
            setFormData(prev => ({ ...prev, image_path: result.filePath }));
            
            URL.revokeObjectURL(previewUrl); // Cleanup memory
        } catch(e) {
            console.error("Error uploading image: ", e);
            // Revert on failure
            setFormData(prev => ({ ...prev, image_path: originalImageRef.current }));
            alert("Failed to upload image.");
            URL.revokeObjectURL(previewUrl);
        } finally {
            setUploading(false);
            if (e.target.value) e.target.value = ''; // Reset input
        }
    }

    // 5. Submit Changes
    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        
        try {
            const token = sessionStorage.getItem("userToken");

            // PUT /api/Posts/{id}
            const res = await fetch(`/api/Posts/${post_id}`, {
                method: "PUT",
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    description: formData.description,
                    img_path: formData.image_path, // Send updated image path
                })
            });

            if (!res.ok) {
                throw new Error(`Update error: ${res.status}`);
            }

            // Navigate back on success
            navigate(-1); 

        } catch(e) {
            console.error("Error updating post: ", e);
            alert("Failed to update post.");
        }
    };

    return (
        <div className="edit-post-container">
            <form className="edit-post-card" onSubmit={handleSubmit}>
                <h2>Edit Post</h2>

                {/* Post Image Section (Click to edit) */}
                <div className="post-image-wrapper" onClick={handleImageClick}>
                    <img 
                        src={formData.image_path} 
                        alt="Post Preview" 
                        className={`post-preview-img ${uploading ? 'dimmed' : ''}`}
                    />
                    <div className="overlay-hint">Click to change image</div>
                    
                    <input
                        type="file"
                        accept="image/*"
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
                        value={formData.description}
                        onChange={handleChange}
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