export function setup()
{
    window.mostlylucid.simplemde.initialize('commenteditor', true);
    
}

export function setValues(evt)
{
    const button =evt.currentTarget;
    let element = window.mostlylucid.simplemde.getinstance('commenteditor');
    let content = element.value();
    let email = document.getElementById("Email");
    let name = document.getElementById("Name");
    let blogPostId = document.getElementById("BlogPostId");
    
    var values = {
        "content": content,
        "email": email.value,
        "name": name.value,
        "blogPostId": blogPostId.value
    };
    button.setAttribute('hx-vals', JSON.stringify(values));
   //button.click();
}