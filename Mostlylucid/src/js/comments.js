export function comments() {
    return{
        setValues :setValues,
        setup : setup
    }

    function setup ()  {
        if (mostlylucid.simplemde && typeof mostlylucid.simplemde.initialize === 'function') {
            mostlylucid.simplemde.initialize('commenteditor', true);
        } else {
            console.error("simplemde is not initialized correctly.");
        }
    };

    function setValues (evt)  {
        const button = evt.currentTarget;
        const element = mostlylucid.simplemde.getinstance('commenteditor');
        const content = element.value();
        const email = document.getElementById("Email");
        const name = document.getElementById("Name");
        const blogPostId = document.getElementById("BlogPostId");

        const values = {
            content: content,
            email: email.value,
            name: name.value,
            blogPostId: blogPostId.value
        };

        button.setAttribute('hx-vals', JSON.stringify(values));
    };



    // Ensure setup is called when DOM is fully loaded
    document.addEventListener('DOMContentLoaded', setup);

}