export interface PostType{
    id: number,
    owner: string,
    img_path: string,
    nr_likes: number,
    has_liked: boolean,
    nr_comm: number,
}

export interface UserSettingsType{
    id: number,
    userName: string,
    name: string,
    description: string,
    profile_image: string,
}

export interface UserProfileType{
    id: number,
    userName: string,
    name: string,
    description: string
    nr_followers: number,
    nr_following: number,
    posts: PostType[],
}


// Define this alongside PostType or in a dedicated types file

export interface PostApiType {
    id: number;
    owner: string;
    image_path: string; // Matches 'img_path' in your client PostType
    nr_likes: number;
    nr_comms: number; // Matches 'nr_comm' in your client PostType
    // The API response likely contains other fields, but these are the ones you use
}